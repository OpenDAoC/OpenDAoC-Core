using DOL.GS;

namespace DOL.AI.Brain;

public class ErisusBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ErisusBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 300;
        ThinkInterval = 1000;
    }
    public override void Think()
    {
        if(HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1500))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "ErisusBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}