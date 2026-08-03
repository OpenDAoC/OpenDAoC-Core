using DOL.AI.Brain;

namespace DOL.GS
{
    public class WaterSpiderGleek : TimeDependentSpawnNpc
    {
        public WaterSpiderGleek() : base(new TimeDependentSpawnBrain()) { }

        protected override bool ShouldBeVisible()
        {
            uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
            return hour is >= 10 and < 13;
        }
    }
}
