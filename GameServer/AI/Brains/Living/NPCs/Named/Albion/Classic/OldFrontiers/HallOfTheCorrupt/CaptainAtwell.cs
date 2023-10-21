using Core.GS.Enums;

namespace Core.GS.AI.Brains;

public class CaptainAtwellBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public CaptainAtwellBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 1500;
    }
    public static bool reset_darra = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
        }
        if (Body.IsOutOfTetherRange)
        {
            Body.Health = Body.MaxHealth;
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
        }
        if (Body.InCombat && HasAggro)
        {
            if(Body.TargetObject != null)
            {
                Body.styleComponent.NextCombatBackupStyle = CaptainAtwell.PoleAnytimer;
                Body.styleComponent.NextCombatStyle = CaptainAtwell.AfterParry;
            }
        }
        base.Think();
    }
}