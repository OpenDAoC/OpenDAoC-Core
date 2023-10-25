using Core.GS.Enums;

namespace Core.GS.AI;

#region Cronwort
public class CronwortBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public CronwortBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if(HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(5000))
            {
                if (npc != null && npc.IsAlive && npc.Brain is BreanwortBrain brain)
                {
                    GameLiving target = Body.TargetObject as GameLiving;
                    if (!brain.HasAggro && target.IsAlive && target != null)
                        brain.AddToAggroList(target, 10);
                }
            }
        }
        base.Think();
    }
}
#endregion Cronwort

#region Breanwort add
public class BreanwortBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public BreanwortBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 800;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}
#endregion Breanwort add