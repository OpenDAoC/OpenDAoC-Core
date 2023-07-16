using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Movement;
using FiniteStateMachine;
using log4net;
using static DOL.AI.Brain.StandardMobBrain;

public class StandardMobState : State
{
    protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    protected StandardMobBrain _brain = null;
    protected eFSMStateType _id;

    public eFSMStateType ID => _id;

    public StandardMobState(FSM fsm, StandardMobBrain brain) : base(fsm)
    {
        _brain = brain;
    }
}

public class StandardMobState_IDLE : StandardMobState
{
    public StandardMobState_IDLE(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering IDLE");

        base.Enter();
    }

    public override void Think()
    {
        if (_brain.HasPatrolPath())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
            return;
        }

        if (_brain.Body.CanRoam)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
            return;
        }

        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        _brain.CheckSpells(eCheckSpellType.Defensive);
        base.Think();
    }
}

public class StandardMobState_WAKING_UP : StandardMobState
{
    public StandardMobState_WAKING_UP(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.WAKING_UP;
    }

    public override void Think()
    {
        if (!_brain.Body.attackComponent.AttackState && _brain.Body.CanRoam)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
            return;
        }

        if (_brain.HasPatrolPath())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        base.Think();
    }
}

public class StandardMobState_AGGRO : StandardMobState
{
    public StandardMobState_AGGRO(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering AGGRO");

        base.Enter();
    }

    public override void Exit()
    {
        if (_brain.Body.attackComponent.AttackState)
            _brain.Body.StopAttack();

        _brain.Body.TargetObject = null;
        base.Exit();
    }

    public override void Think()
    {
        if (!_brain.Body.InCombatInLast(25000) || _brain.Body.IsNearSpawn)
        {
            if (_brain.IsBeyondTetherRange() || !_brain.CheckProximityAggro())
            {
                if (_brain.Body.CurrentWaypoint != null)
                    _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
                else
                    _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);

                return;
            }
        }

        if (_brain.Body.Flags.HasFlag(GameNPC.eFlags.STEALTH))
            _brain.Body.Flags ^= GameNPC.eFlags.STEALTH;

        _brain.AttackMostWanted();
        base.Think();
    }
}

public class StandardMobState_ROAMING : StandardMobState
{
    private const int ROAM_COOLDOWN = 45 * 1000;
    private long _lastRoamTick = 0;

    public StandardMobState_ROAMING(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.ROAMING;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering ROAM");

        base.Enter();
    }

    public override void Think()
    {
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        if (!_brain.Body.IsCasting)
        {
            if (_lastRoamTick + ROAM_COOLDOWN <= GameLoop.GameLoopTime && Util.Chance(DOL.GS.ServerProperties.Properties.GAMENPC_RANDOMWALK_CHANCE))
            {
                _brain.Body.Roam();
                _brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.roaming, _brain.Body);
                _lastRoamTick = GameLoop.GameLoopTime;
            }
        }

        _brain.CheckSpells(eCheckSpellType.Defensive);
        base.Think();
    }
}

public class StandardMobState_RETURN_TO_SPAWN : StandardMobState
{
    public StandardMobState_RETURN_TO_SPAWN(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.RETURN_TO_SPAWN;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering RETURN_TO_SPAWN");

        if (_brain.Body.WasStealthed)
            _brain.Body.Flags |= GameNPC.eFlags.STEALTH;

        _brain.ClearAggroList();
        _brain.Body.ReturnToSpawnPoint();
        base.Enter();
    }

    public override void Think()
    {
        if (!_brain.Body.IsNearSpawn &&
            (!_brain.HasAggro || !_brain.Body.IsEngaging) &&
            (!_brain.Body.IsReturningToSpawnPoint) &&
            _brain.Body.CurrentSpeed == 0)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
            _brain.Body.TurnTo(_brain.Body.SpawnHeading);
            return;
        }

        if (_brain.Body.IsNearSpawn)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
            _brain.Body.TurnTo(_brain.Body.SpawnHeading);
            return;
        }

        base.Think();
    }
}

public class StandardMobState_PATROLLING : StandardMobState
{
    public StandardMobState_PATROLLING(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.PATROLLING;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is PATROLLING");

        _brain.Body.MoveOnPath(_brain.Body.MaxSpeed);
        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        base.Think();
    }
}

public class StandardMobState_DEAD : StandardMobState
{
    public StandardMobState_DEAD(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.DEAD;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} has entered DEAD state");

        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        base.Think();
    }
}
