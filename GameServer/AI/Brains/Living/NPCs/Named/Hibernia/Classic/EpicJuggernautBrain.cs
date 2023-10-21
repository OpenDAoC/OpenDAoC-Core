using Core.GS;

namespace Core.AI.Brain;

public class EpicJuggernautBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public EpicJuggernautBrain() : base()
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
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
            Body.styleComponent.NextCombatStyle = EpicJuggernaut.taunt;
        base.Think();
    }
}