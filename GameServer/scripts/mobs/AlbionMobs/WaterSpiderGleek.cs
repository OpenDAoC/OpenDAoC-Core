using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
    public class WaterSpiderGleek : TimeDependentSpawnNpc
    {
        public WaterSpiderGleek() : base() { }

        protected override ABrain CreateBrain()
        {
            return new WaterSpiderGleekBrain();
        }
    }
}
namespace DOL.AI.Brain
{
    public class WaterSpiderGleekBrain : TimeDependentSpawnBrain
    {
        public WaterSpiderGleekBrain() : base() { }

        protected override bool ShouldBeVisible()
        {
            uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
            return hour is >= 10 and < 13;
        }
    }
}
