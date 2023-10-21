namespace Core.GS.AI.Brains;

public class SergeantEddisonBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SergeantEddisonBrain()
        : base()
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
            this.Body.Health = this.Body.MaxHealth;
            SergeantEddison.IsRanged = false;
        }
        if (Body.IsOutOfTetherRange)
        {
            this.Body.Health = this.Body.MaxHealth;
        }
        base.Think();
    }
}