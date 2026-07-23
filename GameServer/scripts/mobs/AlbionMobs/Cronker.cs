using DOL.AI.Brain;
using DOL.GS;

namespace DOL.GS
{
    public class Cronker : TimeDependentSpawnNpc
    {
        public Cronker() : base(new CronkerBrain()) { }
    }
}

namespace DOL.AI.Brain
{
    public class CronkerBrain : TimeDependentSpawnBrain
    {
        public override void Think()
        {
            base.Think();

            if (HasAggro && Body.TargetObject != null)
            {
                foreach (GameNPC npc in Body.GetNPCsInRadius(1500))
                {
                    if (npc.IsAlive && npc.Name == "giant frog")
                        AddAggroListTo(npc.Brain as StandardMobBrain);
                }
            }
        }

        protected override bool ShouldBeVisible()
        {
            uint hour = WorldMgr.GetCurrentGameTime() / 1000 / 60 / 60;
            return hour is >= 8 and < 14;
        }
    }
}
