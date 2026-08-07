using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class AttackerTracker
    {
        private readonly GameLiving _owner;
        private readonly AttackerCheckTimer _attackerCheckTimer;
        private int _meleeCount = 0;
        private int _playerCount = 0;
        private int _petCount = 0;

        private readonly Dictionary<GameLiving, AttackerInfo> _attackers = new();
        private readonly Lock _lock = new();

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

        public int MeleeCount => Volatile.Read(ref _meleeCount);

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
            _owner = owner;
            _attackerCheckTimer = new AttackerCheckTimer(this);
        }

        public void AddOrUpdate(GameLiving attacker, bool isMelee, long expireTime)
        {
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
                            _meleeCount++;
                        else
                            _meleeCount--;
                    }
                }
                else
                {
                    _attackers.Add(attacker, attackerInfo);
                    _attackerCheckTimer.WakeUp();

                    if (isMelee)
                        _meleeCount++;

                    // The classification of an attacker never changes, so it only needs to be counted when the entry is created.
                    if (attacker is GamePlayer)
                        _playerCount++;
                    else if (attacker is GameSummonedPet)
                        _petCount++;
                }
            }
        }

        public bool ContainsAttacker(GameLiving attacker)
        {
            lock (_lock)
            {
                return _attackers.ContainsKey(attacker);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _attackers.Clear();
                _meleeCount = 0;
                _playerCount = 0;
                _petCount = 0;
            }

            _attackerCheckTimer.Stop();
        }

        private readonly record struct AttackerInfo(bool IsMelee, long ExpireTime);

        private sealed class AttackerCheckTimer : ECSGameTimerWrapperBase
        {
            public const int CHECK_ATTACKERS_INTERVAL = 1000;

            private readonly AttackerTracker _attackerTracker;

            public AttackerCheckTimer(AttackerTracker attackerTracker) : base(attackerTracker._owner)
            {
                _attackerTracker = attackerTracker;
            }

            public void WakeUp()
            {
                if (IsAlive)
                    return;

                Interval = CHECK_ATTACKERS_INTERVAL;
                Start();
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                lock (_attackerTracker._lock)
                {
                    foreach (var pair in _attackerTracker._attackers)
                        TryRemoveAttacker(pair);

                    return _attackerTracker.Count == 0 ? 0 : CHECK_ATTACKERS_INTERVAL;
                }
            }

            private bool TryRemoveAttacker(in KeyValuePair<GameLiving, AttackerInfo> pair)
            {
                if (pair.Value.ExpireTime < GameLoop.GameLoopTime && _attackerTracker._attackers.Remove(pair.Key))
                {
                    if (pair.Value.IsMelee)
                        _attackerTracker._meleeCount--;

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
