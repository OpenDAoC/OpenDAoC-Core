using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class Strangler : GameNPC
    {
        public Strangler() : base()
        {
        }

        public override bool AddToWorld()
        {
            var brain = new StranglerBrain();
            SetOwnBrain(brain);
            return base.AddToWorld();
        }

    }
}

namespace DOL.AI.Brain
{
    public class StranglerBrain : StandardMobBrain
    {
        public override int ThinkInterval
        {
            get { return 3000; }
        }

        public override void Think()
        {
            base.Think();
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(Body, Body, 5201, 0, false, 1);
        }
    }
}
