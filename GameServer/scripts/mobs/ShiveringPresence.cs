using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS.Scripts
{
    public class ShiveringPresence : GameNPC
    {
        public ShiveringPresence() : base()
        {
        }

        public override bool AddToWorld()
        {
            var brain = new ShiveringPresenceBrain();
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
        public override int ThinkInterval
        {
            get { return 3000; }
        }

        public override void Think()
        {
            base.Think();
            foreach (GamePlayer player in Body.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(Body, Body, 152, 0, false, 1);
        }
    }
}
