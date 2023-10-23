namespace Core.GS.AI;

public class RunilBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public RunilBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if(Body.TargetObject != null && HasAggro)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "RunilBaf" && npc.Brain is StandardMobBrain brain)
                {
                    if (brain != null && !brain.HasAggro && target != null && target.IsAlive)
                        brain.AddToAggroList(target, 100);
                }
            }
        }
        base.Think();
    }
}