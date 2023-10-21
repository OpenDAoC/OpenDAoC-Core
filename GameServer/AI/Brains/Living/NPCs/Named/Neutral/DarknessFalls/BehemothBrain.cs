using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class BehemothBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public BehemothBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}