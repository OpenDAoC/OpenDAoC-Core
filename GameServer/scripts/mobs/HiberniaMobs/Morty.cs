using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
    public class Morty : TimeDependentSpawnNpc
    {
        public Morty() : base(new MortyBrain()) { }
    }
}

namespace DOL.AI.Brain
{
    public class MortyBrain : TimeDependentSpawnBrain
    {
        protected override bool ShouldBeVisible()
        {
            uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
            return hour is >= 8 and < 12;
        }
    }
}
