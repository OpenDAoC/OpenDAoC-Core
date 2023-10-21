using DOL.GS;

namespace DOL.AI.Brain;

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
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}