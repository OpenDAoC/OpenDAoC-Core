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
    public class StranglerBrain : AmbientEffectBrain
    {
        protected override ushort AmbientEffectId => 5206;
    }
}
