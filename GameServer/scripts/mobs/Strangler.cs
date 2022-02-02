using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Effects;

namespace DOL.GS.Scripts
{
    public class Strangler : GameNPC
    {

        public Strangler() : base()
        {
            SetOwnBrain(new StranglerBrain());
        }
        
    }
}

namespace DOL.AI.Brain
{
    public class StranglerBrain : StandardMobBrain
    {
        
        DummyEffect effect = new DummyEffect(5201);
        public StranglerBrain()
            : base()
        {

        }
        
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

        public RegionTimer SpellTimer { get; set; }
    }
}
