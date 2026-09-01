using System;
using System.Collections.Generic;
using System.Threading;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public class AttackerTracker
    {
        private readonly GameLiving _owner;
        private readonly AttackerCheckTimer _attackerCheckTimer;
        private readonly Lock _lock = new();
        private int _playerCount;
        private int _petCount;

        private readonly Dictionary<GameLiving, AttackerInfo> _attackers = new();
        private int _meleeAttackerCount;

        private GameLiving _lastInterrupter;
        private long _interruptExpireTime;
        private long _selfInterruptExpireTime;

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _attackers.Count;
                }
            }
        }

        public int MeleeCount => Volatile.Read(ref _meleeAttackerCount);

        public int PlayerCount => Volatile.Read(ref _playerCount);

        public int PetCount => Volatile.Read(ref _petCount);

        public ICollection<GameLiving> Attackers
        {
            get
            {
                List<GameLiving> result = GameLoop.GetListForTick<GameLiving>();

                lock (_lock)
                {
                    result.EnsureCapacity(_attackers.Count);

                    foreach (GameLiving key in _attackers.Keys)
                        result.Add(key);
                }

                return result;
            }
        }

        public AttackerTracker(GameLiving owner)
        {
            ArgumentNullException.ThrowIfNull(owner);

            _owner = owner;
            _attackerCheckTimer = AttackerCheckTimer.Create(this);
        }

        public void AddOrUpdate(GameLiving attacker, bool isMelee, long expireTime)
        {
            ArgumentNullException.ThrowIfNull(attacker);

            if (attacker == _owner)
                return;

            AttackerInfo attackerInfo = new(isMelee, expireTime);

            lock (_lock)
            {
                if (_attackers.TryGetValue(attacker, out AttackerInfo existing))
                {
                    _attackers[attacker] = attackerInfo;

                    if (existing.IsMelee != isMelee)
                    {
                        if (isMelee)
                            _meleeAttackerCount++;
                        else
                            _meleeAttackerCount--;
                    }
                }
                else
                {
                    _attackers.Add(attacker, attackerInfo);
                    _attackerCheckTimer.WakeUp();

                    if (isMelee)
                        _meleeAttackerCount++;

                    if (attacker is GamePlayer)
                        _playerCount++;
                    else if (attacker is GameSummonedPet)
                        _petCount++;
                }
            }
        }

        public bool ContainsAttacker(GameLiving attacker)
        {
            ArgumentNullException.ThrowIfNull(attacker);

            lock (_lock)
            {
                return _attackers.ContainsKey(attacker);
            }
        }

        public void SetInterrupt(GameLiving interrupter, long expireTime)
        {
            ArgumentNullException.ThrowIfNull(interrupter);

            lock (_lock)
            {
                if (_interruptExpireTime >= expireTime)
                    return;

                _lastInterrupter = interrupter;
                _interruptExpireTime = expireTime;
                _attackerCheckTimer.WakeUp();
            }
        }

        public void SetSelfInterrupt(long expireTime)
        {
            lock (_lock)
            {
                _selfInterruptExpireTime = expireTime;
            }
        }

        public bool IsInterrupted(out GameLiving lastInterrupter)
        {
            lastInterrupter = null;

            lock (_lock)
            {
                if (_interruptExpireTime > GameLoop.GameLoopTime)
                {
                    lastInterrupter = _lastInterrupter;
                    return true;
                }
            }

            return false;
        }

        public bool IsSelfInterrupted()
        {
            return Volatile.Read(ref _selfInterruptExpireTime) > GameLoop.GameLoopTime;
        }

        public bool IsInterruptedOrSelfInterrupted()
        {
            return IsInterrupted(out _) || IsSelfInterrupted();
        }

        public long GetInterruptRemainingDuration()
        {
            long interruptTime = Properties.HARD_INTERRUPT_ON_ATTACKED ?
                Math.Max(Volatile.Read(ref _selfInterruptExpireTime), Volatile.Read(ref _interruptExpireTime)) :
                Volatile.Read(ref _selfInterruptExpireTime);
            return Math.Max(0, interruptTime - GameLoop.GameLoopTime);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _attackers.Clear();
                _meleeAttackerCount = 0;
                _playerCount = 0;
                _petCount = 0;
                _lastInterrupter = null;
                _interruptExpireTime = 0;
                _selfInterruptExpireTime = 0;
                _attackerCheckTimer.Stop();
            }
        }

        private bool TryClearInterrupt()
        {
            lock (_lock)
            {
                if (_interruptExpireTime > GameLoop.GameLoopTime)
                    return false;

                _lastInterrupter = null;
            }

            return true;
        }

        private readonly record struct AttackerInfo(bool IsMelee, long ExpireTime);

        private class StandardAttackerCheckTimer : AttackerCheckTimer
        {
            public StandardAttackerCheckTimer(AttackerTracker attackerTracker) : base(attackerTracker) { }

            protected override int OnTick(ECSGameTimer timer)
            {
                lock (_attackerTracker._lock)
                {
                    foreach (var pair in _attackerTracker._attackers)
                        TryRemoveAttacker(pair);

                    return base.OnTick(timer);
                }
            }
        }

        private abstract class AttackerCheckTimer : ECSGameTimerWrapperBase
        {
            private const int CHECK_ATTACKERS_INTERVAL = 1000;

            protected readonly GameLiving _owner;
            protected readonly AttackerTracker _attackerTracker;

            public AttackerCheckTimer(AttackerTracker attackerTracker) : base(attackerTracker._owner)
            {
                _owner = attackerTracker._owner;
                _attackerTracker = attackerTracker;
                Interval = CHECK_ATTACKERS_INTERVAL;
            }

            public static AttackerCheckTimer Create(AttackerTracker attackerTracker)
            {
                return new StandardAttackerCheckTimer(attackerTracker);
            }

            public void WakeUp()
            {
                if (IsAlive)
                    return;

                Start();
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                return _attackerTracker.Count > 0 || !_attackerTracker.TryClearInterrupt() ? Interval : 0;
            }

            protected bool TryRemoveAttacker(KeyValuePair<GameLiving, AttackerInfo> pair)
            {
                AttackerInfo attackerInfo = pair.Value;

                if (attackerInfo.ExpireTime < GameLoop.GameLoopTime && _attackerTracker._attackers.Remove(pair.Key))
                {
                    if (attackerInfo.IsMelee)
                        _attackerTracker._meleeAttackerCount--;

                    if (pair.Key is GamePlayer)
                        _attackerTracker._playerCount--;
                    else if (pair.Key is GameSummonedPet)
                        _attackerTracker._petCount--;

                    return true;
                }

                return false;
            }
        }
    }
}
