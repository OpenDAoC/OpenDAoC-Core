using DOL.GS;

namespace DOL.AI.Brain;

public class YaddaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public YaddaBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (HasAggro && Body.TargetObject != null)
        {
            GameLiving target = Body.TargetObject as GameLiving;
            foreach (GameNpc npc in Body.GetNPCsInRadius(4000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is DramacusBrain brian)
                {
                    if (!brian.HasAggro && brian != null && target != null && target.IsAlive)
                        brian.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}