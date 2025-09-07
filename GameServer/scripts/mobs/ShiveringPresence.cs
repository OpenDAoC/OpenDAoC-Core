using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class ShiveringPresence : GameNPC
    {
        public override bool AddToWorld()
        {
            ShiveringPresenceBrain brain = new();
            SetOwnBrain(brain);
            Model = 966;
            return base.AddToWorld();
        }
    }
}

namespace DOL.AI.Brain
{
    public class ShiveringPresenceBrain : StandardMobBrain
    {
        private ShiveringPresenceTimer _timer;

        public override bool Start()
        {
            if (!base.Start())
                return false;

            _timer ??= new(Body);
            _timer.Start();
            return true;
        }

        public override bool Stop()
        {
            if (!base.Stop())
                return false;

            _timer?.Stop();
            _timer = null;
            return true;
        }
    }

    public class ShiveringPresenceTimer : ECSGameTimerWrapperBase
    {
        private GameNPC _owner;

        public ShiveringPresenceTimer(GameNPC owner) : base(owner)
        {
            _owner = owner;
            Start();
        }

        protected override int OnTick(ECSGameTimer timer)
        {
            foreach (GamePlayer player in _owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(_owner, _owner, 152, 0, false, 1);

            // Delay the next effect by 6~20 seconds.
            return 6000 + Util.Random(7000) + Util.Random(7000);
        }
    }
}
