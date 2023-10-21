using Core.GS.Enums;

namespace Core.GS.AI.Brains;

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
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (HasAggro && Body.TargetObject != null)
            Body.styleComponent.NextCombatStyle = EpicJuggernaut.taunt;
        base.Think();
    }
}