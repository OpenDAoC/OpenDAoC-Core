using Core.GS.Enums;

namespace Core.GS.AI;

public class GreenMawBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GreenMawBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 450;
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
        base.Think();
    }
}

public class GreenMawAddBrain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GreenMawAddBrain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}

public class GreenMawAdd2Brain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GreenMawAdd2Brain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}

public class GreenMawAdd3Brain : StandardMobBrain
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
    public GreenMawAdd3Brain() : base()
    {
        AggroLevel = 100;
        AggroRange = 1000;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        base.Think();
    }
}