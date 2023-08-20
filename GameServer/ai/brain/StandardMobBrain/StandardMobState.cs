using System;
using System.Reflection;
using DOL.GS;
using DOL.GS.ServerProperties;
using log4net;

namespace DOL.AI.Brain
{
    public class StandardMobState : FSMState
    {
        protected static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        protected StandardMobBrain _brain = null;

        public StandardMobState(StandardMobBrain brain) : base()
        {
            _brain = brain;
        }

        public override void Think() { }
        public override void Enter() { }
        public override void Exit() { }
    }

    public class StandardMobState_IDLE : StandardMobState
    {
        public StandardMobState_IDLE(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.IDLE;
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
                _brain.FSM.SetCurrentState( eFSMStateType.ROAMING);
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

            _brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);
            base.Think();
        }
    }

    public class StandardMobState_WAKING_UP : StandardMobState
    {
        public StandardMobState_WAKING_UP(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.WAKING_UP;
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
        private const int LEAVE_WHEN_OUT_OF_COMBAT_FOR = 25000;

        private long _aggroTime = GameLoop.GameLoopTime; // Used to prevent leaving on the first think tick, due to `InCombatInLast` returning false.

        public StandardMobState_AGGRO(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.AGGRO;
        }

        public override void Enter()
        {
            if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
                Console.WriteLine($"{_brain.Body} is entering AGGRO");

            _aggroTime = GameLoop.GameLoopTime;
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
            if (!_brain.HasAggro || (!_brain.Body.InCombatInLast(LEAVE_WHEN_OUT_OF_COMBAT_FOR) && _aggroTime + LEAVE_WHEN_OUT_OF_COMBAT_FOR <= GameLoop.GameLoopTime))
            {
                if (_brain.Body.CurrentWaypoint != null)
                    _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
                else
                    _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);

                return;
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

        public StandardMobState_ROAMING(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.ROAMING;
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
                if (_lastRoamTick + ROAM_COOLDOWN <= GameLoop.GameLoopTime && Util.Chance(Properties.GAMENPC_RANDOMWALK_CHANCE))
                {
                    _brain.Body.Roam(NpcMovementComponent.DEFAULT_WALK_SPEED);
                    _brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.roaming, _brain.Body);
                    _lastRoamTick = GameLoop.GameLoopTime;
                }
            }

            _brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);
            base.Think();
        }
    }

    public class StandardMobState_RETURN_TO_SPAWN : StandardMobState
    {
        public StandardMobState_RETURN_TO_SPAWN(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.RETURN_TO_SPAWN;
        }

        public override void Enter()
        {
            if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
                Console.WriteLine($"{_brain.Body} is entering RETURN_TO_SPAWN");

            if (_brain.Body.WasStealthed)
                _brain.Body.Flags |= GameNPC.eFlags.STEALTH;

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
        public StandardMobState_PATROLLING(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.PATROLLING;
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
        public StandardMobState_DEAD(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.DEAD;
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
}
