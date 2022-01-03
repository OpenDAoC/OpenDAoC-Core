using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Relics;
using FiniteStateMachine;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
public class MistressState : StandardMobState
{
    protected new MistressBrain _brain = null;
    public MistressState(FSM fsm, MistressBrain brain) : base(fsm, brain)
    {
        _brain = brain;
    }

}

public class MistressState_IDLE : MistressState
{
    public MistressState_IDLE(FSM fsm, MistressBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Mistress of Runes {_brain.Body} has entered IDLE");
        }
        base.Enter();
    }

    public override void Think()
    {
        //if we're walking home, do nothing else
        if (_brain.Body.IsReturningHome) return;      

        // If we aren't already aggroing something, look out for
        // someone we can aggro on and attack right away.
        if (!_brain.HasAggro && _brain.AggroLevel > 0)
        {
            _brain.CheckPlayerAggro();
            _brain.CheckNPCAggro();

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

        // If Mistress of Runes has run out of tether range, clear aggro list and let it 
        // return to its spawn point.
        if (_brain.CheckTether())
        {
            //set state to RETURN TO SPAWN
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }
}

public class MistressState_AGGRO : MistressState
{
    public MistressState_AGGRO(FSM fsm, MistressBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Mistress of Runes {_brain.Body} has entered AGGRO on target {_brain.Body.TargetObject}");
        }
        base.Enter();
    }

    public override void Think()
    {      
        if (_brain.PickNearsightTarget()) return;
        if (_brain.PickAoETarget()) return;

        // If Mistress of RUnes has run out of tether range, or has clear aggro list, 
        // let it return to its spawn point.
        if (_brain.CheckTether() || !_brain.HasAggressionTable())
        {
            //set state to RETURN TO SPAWN
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }

}

public class MistressState_RETURN_TO_SPAWN : MistressState
{
    public MistressState_RETURN_TO_SPAWN(FSM fsm, MistressBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.RETURN_TO_SPAWN;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Mistress of Runes {_brain.Body} is returning to spawn");
        }
        _brain.Body.StopFollowing();
        _brain.ClearAggroList();
        _brain.Body.WalkToSpawn();
    }

    public override void Think()
    {
        if (_brain.Body.IsNearSpawn())
        {
            _brain.Body.CancelWalkToSpawn();
            _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        }
    }
}