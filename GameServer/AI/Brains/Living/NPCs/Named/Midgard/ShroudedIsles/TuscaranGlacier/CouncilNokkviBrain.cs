using Core.GS;

namespace Core.AI.Brain
{
    public class CouncilNokkviBrain : StandardMobBrain
    {
        private static readonly log4net.ILog log =
            log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public CouncilNokkviBrain()
            : base()
        {
            AggroLevel = 100;
            AggroRange = 600;
            ThinkInterval = 2000;
        }
        public static bool IsPulled2 = false;
        public override void OnAttackedByEnemy(AttackData ad)
        {
            if (IsPulled2 == false)
            {
                foreach (GameNpc Vagn in Body.GetNPCsInRadius(1800))
                {
                    if (Vagn != null && Vagn.IsAlive && Vagn.Brain is CouncilVagnBrain)
                        AddAggroListTo(Vagn.Brain as CouncilVagnBrain);
                }
                IsPulled2 = true;
            }
            base.OnAttackedByEnemy(ad);
        }

        public override void Think()
        {
            if (!CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                FiniteStateMachine.SetCurrentState(EFSMStateType.RETURN_TO_SPAWN);
                Body.Health = Body.MaxHealth;
                IsPulled2 = false;
            }

            if (Body.IsOutOfTetherRange)
            {
                Body.Health = Body.MaxHealth;
                ClearAggroList();
            }
            else if (Body.InCombatInLast(30 * 1000) == false && this.Body.InCombatInLast(35 * 1000))
            {
                Body.Health = Body.MaxHealth;
            }
            base.Think();
        }
    }
}