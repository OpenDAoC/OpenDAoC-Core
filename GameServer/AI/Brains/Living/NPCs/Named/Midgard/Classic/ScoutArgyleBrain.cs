using DOL.GS;

namespace DOL.AI.Brain;

public class ScoutArgyleBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public ScoutArgyleBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if(HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(1000))
            {
                if (npc != null && npc.IsAlive && npc.PackageID == "ScoutArgyleBaf")
                    AddAggroListTo(npc.Brain as StandardMobBrain);
            }
        }
        base.Think();
    }
}