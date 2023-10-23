using Core.GS.Enums;

namespace Core.GS.AI;

public class JehrogBysenBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public JehrogBysenBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 600;
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
        if (HasAggro && Body.TargetObject != null)
        {
            foreach (GameNpc npc in Body.GetNPCsInRadius(2500))
            {
                if (npc != null)
                {
                    if (npc.IsAlive && npc.Brain is UlorBysenBrain brain)
                    {
                        GameLiving target = Body.TargetObject as GameLiving;
                        if (!brain.HasAggro && target != null && target.IsAlive)
                            brain.AddToAggroList(target, 10);
                    }
                }
            }
        }
        base.Think();
    }
}