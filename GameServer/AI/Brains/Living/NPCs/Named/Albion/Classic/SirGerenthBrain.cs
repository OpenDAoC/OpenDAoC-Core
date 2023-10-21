using Core.GS;

namespace Core.AI.Brain;

public class SirGerenthBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public SirGerenthBrain() : base()
    {
        AggroLevel = 40;
        AggroRange = 400;
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            if (Body.HealthPercent <= 66)
            {
                foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
                {
                    if (npc != null && npc.IsAlive && npc.PackageID == "SirGerenthBaf")
                        AddAggroListTo(npc.Brain as StandardMobBrain);
                }
            }
        }
        base.Think();
    }
}