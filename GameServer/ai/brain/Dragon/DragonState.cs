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
public class DragonState : StandardMobState
{
    protected new DragonBrain _brain = null;
    public DragonState(FSM fsm, DragonBrain brain) : base(fsm, brain)
    {
        _brain = brain;
    }
}

public class DragonState_WAKING_UP : DragonState {
    public DragonState_WAKING_UP(FSM fsm, DragonBrain brain) : base(fsm, brain) {
        _id = eFSMStateType.WAKING_UP;
    }

    public override void Think() {
        _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        base.Think();
    }
}

public class DragonState_IDLE : DragonState
{
    public DragonState_IDLE(FSM fsm, DragonBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Dragon {_brain.Body} has entered IDLE");
        }
        base.Enter();
    }

    public override void Think()
    {
        //if we're walking home, do nothing else
        if (_brain.Body.IsReturningHome) return;

        //if dragon is full health, reset the encounter stages
        if (_brain.Body.HealthPercent == 100 && _brain.Stage < 10)
            _brain.Stage = 10;

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

        // If dragon has run out of tether range, clear aggro list and let it 
        // return to its spawn point.
        if (_brain.CheckTether())
        {
            //set state to RETURN TO SPAWN
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }
}

public class DragonState_AGGRO : DragonState
{
    public DragonState_AGGRO(FSM fsm, DragonBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Dragon {_brain.Body} has entered AGGRO on target {_brain.Body.TargetObject}");
        }
        base.Enter();
    }

    public override void Think()
    {

        if (_brain.CheckHealth()) return;
        if (_brain.PickGlareTarget()) return;
        _brain.PickThrowTarget();

        // If dragon has run out of tether range, or has clear aggro list, 
        // let it return to its spawn point.
        if (_brain.CheckTether() || !_brain.HasAggressionTable())
        {
            //set state to RETURN TO SPAWN
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }
}

public class DragonState_RETURN_TO_SPAWN : DragonState
{
    public DragonState_RETURN_TO_SPAWN(FSM fsm, DragonBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.RETURN_TO_SPAWN;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Dragon {_brain.Body} is returning to spawn");
        }
        _brain.Body.StopFollowing();
        GameDragon dragon = _brain.Body as GameDragon;
        if (dragon != null)
        {
            dragon.PrepareToStun();
        }

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