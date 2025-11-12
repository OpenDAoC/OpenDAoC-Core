using System.Collections.Generic;
using System.Threading;

namespace DOL.GS
{
    public class AttackerTracker
    {
        private readonly GameLiving _owner;
        private readonly AttackerCheckTimer _attackerCheckTimer;
        private int _meleeCount = 0;

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
            _attackerCheckTimer = AttackerCheckTimer.Create(this);
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
            }

            _attackerCheckTimer.Stop();
        }

        private readonly struct AttackerInfo
        {
            public readonly bool IsMelee;
            public readonly long ExpireTime;

            public AttackerInfo(bool isMelee, long expireTime)
            {
                IsMelee = isMelee;
                ExpireTime = expireTime;
            }
        }

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

        private class EpicNpcAttackerCheckTimer : AttackerCheckTimer
        {
            private IGameEpicNpc _epicNpc;

            public EpicNpcAttackerCheckTimer(AttackerTracker attackerTracker) : base(attackerTracker)
            {
                _epicNpc = attackerTracker._owner as IGameEpicNpc;
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                // Update `ArmorFactorScalingFactor`.
                double armorFactorScalingFactor = _epicNpc.DefaultArmorFactorScalingFactor;
                int petCount = 0;

                lock (_attackerTracker._lock)
                {
                    foreach (var pair in _attackerTracker._attackers)
                    {
                        if (TryRemoveAttacker(pair))
                            continue;

                        if (pair.Key is GamePlayer)
                            armorFactorScalingFactor -= 0.04;
                        else if (pair.Key is GameSummonedPet && petCount <= _epicNpc.ArmorFactorScalingFactorPetCap)
                        {
                            armorFactorScalingFactor -= 0.01;
                            petCount++;
                        }

                        if (armorFactorScalingFactor < 0.4)
                        {
                            armorFactorScalingFactor = 0.4;
                            break;
                        }
                    }

                    _epicNpc.ArmorFactorScalingFactor = armorFactorScalingFactor;
                    return base.OnTick(timer);
                }
            }
        }

        private abstract class AttackerCheckTimer : ECSGameTimerWrapperBase
        {
            public const int CHECK_ATTACKERS_INTERVAL = 1000;

            protected readonly GameLiving _owner;
            protected readonly AttackerTracker _attackerTracker;

            public AttackerCheckTimer(AttackerTracker attackerTracker) : base(attackerTracker._owner)
            {
                _owner = attackerTracker._owner;
                _attackerTracker = attackerTracker;
            }

            public static AttackerCheckTimer Create(AttackerTracker attackerTracker)
            {
                if (attackerTracker._owner is IGameEpicNpc)
                    return new EpicNpcAttackerCheckTimer(attackerTracker);
                else
                    return new StandardAttackerCheckTimer(attackerTracker);
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
                return _attackerTracker.Count == 0 ? 0 : CHECK_ATTACKERS_INTERVAL;
            }

            protected bool TryRemoveAttacker(in KeyValuePair<GameLiving, AttackerInfo> pair)
            {
                if (pair.Value.ExpireTime < GameLoop.GameLoopTime && _attackerTracker._attackers.Remove(pair.Key))
                {
                    if (pair.Value.IsMelee)
                        _attackerTracker._meleeCount--;

                    return true;
                }

                return false;
            }
        }
    }
}
