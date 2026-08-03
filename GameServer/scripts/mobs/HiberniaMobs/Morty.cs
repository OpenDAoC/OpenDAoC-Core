using DOL.AI.Brain;

namespace DOL.GS
{
    public class Morty : TimeDependentSpawnNpc
    {
        public Morty() : base(new TimeDependentSpawnBrain()) { }

        protected override bool ShouldBeVisible()
        {
            uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
            return hour is >= 8 and < 12;
        }
    }
}
