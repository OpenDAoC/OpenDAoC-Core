using System;
using DOL.Events;

namespace DOL.GS.RealmAbilities.Statics
{
    public abstract class RealmAbilityStaticItemBase : GameStaticItem
    {
        protected GamePlayer _caster;
        private long _expireTick;
        private protected ushort _radius;
        private int _pulseFrequency;
        private ECSGameTimer _timer;

        public int CurrentPulse { get; private set; }

        public void CreateStatic(GamePlayer caster, Point3D position, int duration, int pulseFrequency, ushort radius)
        {
            _expireTick = duration * 1000 + GameLoop.GameLoopTime;
            _caster = caster;
            _radius = radius;
            _pulseFrequency = pulseFrequency;
            Name = GetStaticName();
            Model = GetStaticModel();
            X = position.X;
            Y = position.Y;
            Z = position.Z;
            CurrentRegionID = _caster.CurrentRegionID;
            Level = caster.Level;
            Realm = caster.Realm;
            AddToWorld();
        }

        public override bool AddToWorld()
        {
            if (!base.AddToWorld())
                return false;

            _timer = new(this, PulseTimer, _pulseFrequency * 1000);
            GameEventMgr.AddHandler(_caster, GameObjectEvent.RemoveFromWorld, PlayerLeftWorld);
            return true;
        }

        public override bool RemoveFromWorld()
        {
            if (!base.RemoveFromWorld())
                return false;

            _timer?.Stop();
            GameEventMgr.RemoveHandler(GameObjectEvent.RemoveFromWorld, PlayerLeftWorld);
            return true;
        }

        protected abstract string GetStaticName();
        protected abstract ushort GetStaticModel();
        protected abstract ushort GetStaticEffect();
        protected abstract void CastSpell(GameLiving target);

        private int PulseTimer(ECSGameTimer timer)
        {
            CurrentPulse++;

            foreach (GamePlayer target in GetPlayersInRadius(_radius))
                CastSpell(target);

            foreach (GameNPC npc in GetNPCsInRadius(_radius))
                CastSpell(npc);

            if (GameLoop.GameLoopTime >= _expireTick)
            {
                RemoveFromWorld();
                return 0;
            }

            return timer.Interval;
        }

        private void PlayerLeftWorld(DOLEvent e, object sender, EventArgs args)
        {
            if (_caster == sender)
                RemoveFromWorld();
        }
    }
}
