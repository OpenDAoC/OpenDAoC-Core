namespace Core.GS.AI;

public class BerthaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BerthaBrain() : base()
    {
        AggroLevel = 0;
        AggroRange = 400;
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "BerthaBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}