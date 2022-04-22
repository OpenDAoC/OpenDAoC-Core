using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using DOL.GS.Movement;
using FiniteStateMachine;
using log4net;
using System;
using System.Collections.Generic;
using System.Reflection;
using static DOL.AI.Brain.StandardMobBrain;

public class StandardMobState : State
{
    public eFSMStateType ID { get { return _id; } }
    protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    protected StandardMobBrain _brain = null;
    protected eFSMStateType _id;


    public StandardMobState(FSM fsm, StandardMobBrain brain) : base(fsm)
    {
        _brain = brain;
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Think()
    {
        base.Think();
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
        {
            Console.WriteLine($"{_brain.Body} is entering IDLE");
        }
        _brain.CheckForProximityAggro = true;
        base.Enter();
    }

    public override void Think()
    {

        //if DEAD, bail out of calc
        //if HP < 0, set state to DEAD

        //check if has patrol
        //Mob will now always walk on their path
        if (_brain.HasPatrolPath())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
            return;
        }

        if (_brain.CanRandomWalk)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
            return;
        }

        // check for returning to home if to far away
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        //if aggroList > 0,
        //setStatus = aggro
        if (_brain.HasAggressionTable())
        {
            //_brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.fighting, _brain.Body.TargetObject as GameLiving);
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        //cast self buffs if applicable
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

    public override void Enter()
    {
        //Console.WriteLine($"Entering WAKEUP for {_brain}");
        base.Enter();
    }

    public override void Think()
    {
        //Console.WriteLine($"{_brain.Body} is WAKING_UP");
        //if allowed to roam,
        //set state == ROAMING
        if (!_brain.Body.attackComponent.AttackState && _brain.CanRandomWalk)
        {
            _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
            return;
        }

        //if patrol path,
        //set state == PATROLLING
        if (_brain.HasPatrolPath())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
            return;
        }

        //if aggroList > 0,
        //setStatus = aggro
        if (_brain.HasAggressionTable())
        {
            //_brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.fighting, _brain.Body.TargetObject as GameLiving);
            //_brain.AttackMostWanted();
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        //else,
        //set state = IDLE
        _brain.m_fsm.SetCurrentState(eFSMStateType.IDLE);

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
        //enable attack component
        //enable spell component
        if (_brain.Body.attackComponent == null) { _brain.Body.attackComponent = new DOL.GS.AttackComponent(_brain.Body); }
        EntityManager.AddComponent(typeof(AttackComponent), _brain.Body);
        if (_brain.Body.castingComponent == null) { _brain.Body.castingComponent = new DOL.GS.CastingComponent(_brain.Body); }
        EntityManager.AddComponent(typeof(CastingComponent), _brain.Body);

        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"{_brain.Body} is entering AGGRO");
        }
        _brain.CheckForProximityAggro = true;
        //_brain.AttackMostWanted();

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
        // check for returning to home if to far away
        if (!(_brain is KeepGuardBrain) && _brain.IsBeyondTetherRange() && !_brain.Body.InCombatInLast(25000))
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        //if no aggro targets, set State = RETURN_TO_SPAWN
        if (!_brain.HasAggressionTable())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        _brain.AttackMostWanted();

        base.Think();
    }
}

public class StandardMobState_ROAMING : StandardMobState
{
    private int _roamCooldown = 45 * 1000;
    private long _lastRoamTick = 0;

    public StandardMobState_ROAMING(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.ROAMING;
    }

    public override void Enter()
    {
        if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
        {
            Console.WriteLine($"{_brain.Body} is entering ROAM");
        }
        base.Enter();
    }

    public override void Think()
    {
        // check for returning to home if to far away
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            return;
        }

        //if aggroList > 0,
        //setStatus = aggro
        if (_brain.HasAggressionTable())
        {
            //_brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.fighting, _brain.Body.TargetObject as GameLiving);
            //_brain.AttackMostWanted();
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        //if randomWalkChance,
        //find new point
        //walk to point
        if (Util.Chance(DOL.GS.ServerProperties.Properties.GAMENPC_RANDOMWALK_CHANCE) && _lastRoamTick + _roamCooldown <= GameLoop.GameLoopTime )
        {
            IPoint3D target = _brain.CalcRandomWalkTarget();
            if (target != null)
            {
                if (Util.IsNearDistance(target.X, target.Y, target.Z, _brain.Body.X, _brain.Body.Y, _brain.Body.Z, GameNPC.CONST_WALKTOTOLERANCE))
                {
                    _brain.Body.TurnTo(_brain.Body.GetHeading(target));
                }
                else
                {
                    _brain.Body.WalkTo(target, 50);
                }

                _brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.roaming, _brain.Body);
            }
            _lastRoamTick = GameLoop.GameLoopTime;
        }


        //cast self buffs if applicable
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
        {
            Console.WriteLine($"{_brain.Body} is entering RETURN_TO_SPAWN");
        }
        if (_brain.Body.WasStealthed)
            _brain.Body.Flags ^= GameNPC.eFlags.STEALTH;
        _brain.ClearAggroList();
        _brain.CheckForProximityAggro = false;
        _brain.Body.WalkToSpawn();
        base.Enter();
    }

    public override void Exit()
    {
        _brain.CheckForProximityAggro = true;

        base.Exit();
    }

    public override void Think()
    {
        if (_brain.Body.IsNearSpawn())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
            _brain.Body.ResetHeading();
            return;
        }
        if (_brain.Body.InCombat || _brain.HasAggressionTable())
        {
            _brain.Body.CancelWalkToSpawn();
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
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
        {
            Console.WriteLine($"{_brain.Body} is PATROLLING");
        }
        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        // check for returning to home if to far away
        if (_brain.IsBeyondTetherRange())
        {
            _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
        }

        //if aggroList > 0,
        //setStatus = aggro
        if (_brain.HasAggressionTable())
        {
            //_brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.fighting, _brain.Body.TargetObject as GameLiving);
            _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            return;
        }

        //handle patrol logic
        PathPoint path = MovementMgr.LoadPath(_brain.Body.PathID);
        if (path != null)
        {
            _brain.Body.CurrentWayPoint = path;
            _brain.Body.MoveOnPath((short)path.MaxSpeed);
        }
        else
        {
            log.ErrorFormat("Path {0} not found for mob {1}.", _brain.Body.PathID, _brain.Body.Name);
            _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
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
        {
            Console.WriteLine($"{_brain.Body} has entered DEAD state");
        }
        _brain.ClearAggroList();
        base.Enter();
    }

    public override void Think()
    {
        _brain.FSM.SetCurrentState(eFSMStateType.WAKING_UP);
        base.Think();
    }
}
