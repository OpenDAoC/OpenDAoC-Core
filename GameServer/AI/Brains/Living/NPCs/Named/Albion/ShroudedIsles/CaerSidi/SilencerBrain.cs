using Core.GS.Enums;

namespace Core.GS.AI;

public class SilencerBrain : StandardMobBrain
{
    private static readonly log4net.ILog log =
        log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

    public SilencerBrain()
        : base()
    {
        AggroLevel = 100;
        AggroRange = 500;
        ThinkInterval = 5000;
        CanBAF = false;
    }
    private bool ClearAttackers = false;
    public override void Think()
    {
        if (!CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            Silencer.attackers_count = 0;
            //Silencer silencer = new Silencer();
            if (!ClearAttackers)
            {
                if (Silencer.attackers.Count > 0)
                {
                    Silencer.attackers.Clear();
                    ClearAttackers = true;
                }
            }
        }
        if (HasAggro && Body.TargetObject != null)
            ClearAttackers = false;
        if (Body.IsOutOfTetherRange)
        {
            FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            Body.Health = Body.MaxHealth;
            ClearAggroList();
        }
        else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
        {
            Body.Health = Body.MaxHealth;
            Body.Model = 934;
        }
        base.Think();
    }
}