namespace Core.GS.AI.Brains;

public class LieutenantElvaBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public LieutenantElvaBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 400;
        ThinkInterval = 1500;
    }
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
            this.Body.Health = this.Body.MaxHealth;
        }
        if (Body.InCombat && HasAggro)
        {
            if (Body.TargetObject != null)
            {
                GameLiving living = Body.TargetObject as GameLiving;
            }
        }
        base.Think();
    }
}