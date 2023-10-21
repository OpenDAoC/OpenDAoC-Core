using System;
using System.Reflection;
using Core.GS.AI.Brains;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Server;
using log4net;

namespace Core.GS.AI.States;

public class StandardNpcState : FsmState
{
    protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    protected StandardMobBrain _brain = null;

    public StandardNpcState(StandardMobBrain brain) : base()
    {
        _brain = brain;
    }

    public override void Think() { }
    public override void Enter() { }
    public override void Exit() { }
}

public class StandardNpcStateIdle : StandardNpcState
{
    public StandardNpcStateIdle(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.IDLE;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering IDLE");

        base.Enter();
    }

    public override void Think()
    {
        if (_brain.HasPatrolPath())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.PATROLLING);
            return;
        }

        if (_brain.Body.CanRoam)
        {
            _brain.FiniteStateMachine.SetCurrentState( EFsmStateType.ROAMING);
            return;
        }

        if (_brain.IsBeyondTetherRange())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.AGGRO);
            return;
        }

        _brain.CheckSpells(ECheckSpellType.Defensive);
        base.Think();
    }
}

public class StandardNpcStateWakingUp : StandardNpcState
{
    public StandardNpcStateWakingUp(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.WAKING_UP;
    }

    public override void Think()
    {
        if (!_brain.Body.attackComponent.AttackState && _brain.Body.CanRoam)
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.ROAMING);
            return;
        }

        if (_brain.HasPatrolPath())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.PATROLLING);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.AGGRO);
            return;
        }

        _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.IDLE);
        base.Think();
    }
}

public class StandardNpcStateAggro : StandardNpcState
{
    private const int LEAVE_WHEN_OUT_OF_COMBAT_FOR = 25000;

    private long _aggroTime = GameLoopMgr.GameLoopTime; // Used to prevent leaving on the first think tick, due to `InCombatInLast` returning false.

    public StandardNpcStateAggro(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.AGGRO;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering AGGRO");

        _aggroTime = GameLoopMgr.GameLoopTime;
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
        if (!_brain.HasAggro || (!_brain.Body.InCombatInLast(LEAVE_WHEN_OUT_OF_COMBAT_FOR) && _aggroTime + LEAVE_WHEN_OUT_OF_COMBAT_FOR <= GameLoopMgr.GameLoopTime))
        {
            if (!_brain.Body.IsMezzed && !_brain.Body.IsStunned)
            {
                if (_brain.Body.CurrentWaypoint != null)
                    _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.PATROLLING);
                else
                    _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            }

            return;
        }

        if (_brain.Body.Flags.HasFlag(ENpcFlags.STEALTH))
            _brain.Body.Flags ^= ENpcFlags.STEALTH;

        _brain.AttackMostWanted();
        base.Think();
    }
}

public class StandardNpcStateRoaming : StandardNpcState
{
    private const int ROAM_COOLDOWN = 45 * 1000;
    private long _lastRoamTick = 0;

    public StandardNpcStateRoaming(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.ROAMING;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering ROAM");

        base.Enter();
    }

    public override void Think()
    {
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
            return;
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.AGGRO);
            return;
        }

        if (!_brain.Body.IsCasting)
        {
            if (_lastRoamTick + ROAM_COOLDOWN <= GameLoopMgr.GameLoopTime && Util.Chance(ServerProperty.GAMENPC_RANDOMWALK_CHANCE))
            {
                _brain.Body.Roam(NpcMovementComponent.DEFAULT_WALK_SPEED);
                _brain.Body.FireAmbientSentence(EAmbientNpcTrigger.roaming, _brain.Body);
                _lastRoamTick = GameLoopMgr.GameLoopTime;
            }
        }

        _brain.CheckSpells(ECheckSpellType.Defensive);
        base.Think();
    }
}

public class StandardNpcStateReturnToSpawn : StandardNpcState
{
    public StandardNpcStateReturnToSpawn(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.RETURN_TO_SPAWN;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is entering RETURN_TO_SPAWN");

        if (_brain.Body.WasStealthed)
            _brain.Body.Flags |= ENpcFlags.STEALTH;

        _brain.ClearAggroList();
        _brain.Body.ReturnToSpawnPoint(NpcMovementComponent.DEFAULT_WALK_SPEED);
        base.Enter();
    }

    public override void Think()
    {
        if (!_brain.Body.IsNearSpawn &&
            (!_brain.HasAggro || !_brain.Body.IsEngaging) &&
            (!_brain.Body.IsReturningToSpawnPoint) &&
            _brain.Body.CurrentSpeed == 0)
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.WAKING_UP);
            _brain.Body.TurnTo(_brain.Body.SpawnHeading);
            return;
        }

        if (_brain.Body.IsNearSpawn)
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.WAKING_UP);
            _brain.Body.TurnTo(_brain.Body.SpawnHeading);
            return;
        }

        base.Think();
    }
}

public class StandardNpcStatePatrolling : StandardNpcState
{
    public StandardNpcStatePatrolling(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.PATROLLING;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} is PATROLLING");

        _brain.Body.MoveOnPath(_brain.Body.MaxSpeed);
        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.RETURN_TO_SPAWN);
        }

        if (_brain.CheckProximityAggro())
        {
            _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.AGGRO);
            return;
        }

        base.Think();
    }
}

public class StandardNpcStateDead : StandardNpcState
{
    public StandardNpcStateDead(StandardMobBrain brain) : base(brain)
    {
        StateType = EFsmStateType.DEAD;
    }

    public override void Enter()
    {
        if (Diagnostics.StateMachineDebugEnabled)
            Console.WriteLine($"{_brain.Body} has entered DEAD state");

        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        _brain.FiniteStateMachine.SetCurrentState(EFsmStateType.WAKING_UP);
        base.Think();
    }
}