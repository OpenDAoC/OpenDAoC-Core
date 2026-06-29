using DOL.GS;

namespace DOL.AI.Brain
{
    public abstract class ArosState : StandardMobState
    {
        protected new ArosBrain _brain = null;

        public ArosState(ArosBrain brain) : base(brain)
        {
            _brain = brain;
        }
    }

    public class ArosState_IDLE : ArosState
    {
        public override eFSMStateType StateType => eFSMStateType.IDLE;

        public ArosState_IDLE(ArosBrain brain) : base(brain) { }

        public override void Think()
        {
            //if Aros is full health, reset the encounter stages
            if (_brain.Body.HealthPercent == 100 && _brain.Stage < 10)
                _brain.Stage = 10;

            // If we aren't already aggroing something, look out for
            // someone we can aggro on and attack right away.
            if (!_brain.HasAggro && _brain.AggroLevel > 0)
            {
                _brain.CheckProximityAggro();

                if (_brain.HasAggro)
                {
                    //Set state to AGGRO
                    _brain.AttackMostWanted();
                    _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                    return;
                }
                else
                {
                    if (_brain.Body.attackComponent.AttackState)
                        _brain.Body.StopAttack();

                    _brain.Body.TargetObject = null;
                }
            }

            // If Aros the Spiritmaster has run out of tether range, clear aggro list and let it 
            // return to its spawn point.
            if (_brain.CheckTether())
            {
                //set state to RETURN TO SPAWN
                _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            }
        }
    }

    public class ArosState_AGGRO : ArosState
    {
        public override eFSMStateType StateType => eFSMStateType.AGGRO;

        public ArosState_AGGRO(ArosBrain brain) : base(brain) { }

        public override void Think()
        {
            if (_brain.CheckHealth()) return;
            if (_brain.PickDebuffTarget()) return;

            // If Aros the Spiritmaster has run out of tether range, or has clear aggro list, 
            // let it return to its spawn point.
            if (_brain.CheckTether() || !_brain.CheckProximityAggro())
            {
                //set state to RETURN TO SPAWN
                _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            }
        }

    }
}
