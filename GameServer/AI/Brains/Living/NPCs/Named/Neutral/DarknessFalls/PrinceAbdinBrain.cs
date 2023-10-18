using System.Reflection;
using DOL.GS;
using log4net;

namespace DOL.AI.Brain;

public class PrinceAbdinBrain : StandardMobBrain
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public PrinceAbdinBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 850;
    }

    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        base.Think();
    }
}