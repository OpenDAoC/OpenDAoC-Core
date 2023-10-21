using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.AI.Brains;

public class HighLordSaeorBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public HighLordSaeorBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}