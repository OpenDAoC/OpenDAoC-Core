using System;
using DOL.GS;

namespace DOL.AI.Brain;

public class ArosTheSpiritmasterState : StandardNpcState
{
    protected new ArosTheSpiritmasterBrain _brain = null;

    public ArosTheSpiritmasterState(ArosTheSpiritmasterBrain brain) : base(brain)
    {
        _brain = brain;
    }
}

public class ArosTheSpiritmasterStateIdle : ArosTheSpiritmasterState
{
    public ArosTheSpiritmasterStateIdle(ArosTheSpiritmasterBrain brain) : base(brain)
    {
        StateType = eFSMStateType.IDLE;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Aros the Spiritmaster {_brain.Body} has entered IDLE");
        }
        base.Enter();
    }

    public override void Think()
    {
        //if we're walking home, do nothing else
        if (_brain.Body.IsReturningToSpawnPoint) return;

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
                _brain.FiniteStateMachine.SetCurrentState(eFSMStateType.AGGRO);
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
            _brain.FiniteStateMachine.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }
}

public class ArosTheSpiritmasterStateAggro : ArosTheSpiritmasterState
{
    public ArosTheSpiritmasterStateAggro(ArosTheSpiritmasterBrain brain) : base(brain)
    {
        StateType = eFSMStateType.AGGRO;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Aros the Spiritmaster {_brain.Body} has entered AGGRO on target {_brain.Body.TargetObject}");
        }
        base.Enter();
    }

    public override void Think()
    {
        if (_brain.CheckHealth()) return;
        if (_brain.PickDebuffTarget()) return;

        // If Aros the Spiritmaster has run out of tether range, or has clear aggro list, 
        // let it return to its spawn point.
        if (_brain.CheckTether() || !_brain.CheckProximityAggro())
        {
            //set state to RETURN TO SPAWN
            _brain.FiniteStateMachine.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }
    }
}

public class ArosTheSpiritmasterStateReturnToSpawn : ArosTheSpiritmasterState
{
    public ArosTheSpiritmasterStateReturnToSpawn(ArosTheSpiritmasterBrain brain) : base(brain)
    {
        StateType = eFSMStateType.RETURN_TO_SPAWN;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"Aros the Spiritmaster {_brain.Body} is returning to spawn");
        }
        _brain.Body.StopFollowing();
        _brain.ClearAggroList();
        _brain.Body.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
    }

    public override void Think()
    {
        if (_brain.Body.IsNearSpawn)
        {
            _brain.Body.CancelReturnToSpawnPoint();
            _brain.FiniteStateMachine.SetCurrentState(eFSMStateType.IDLE);
        }
    }
}