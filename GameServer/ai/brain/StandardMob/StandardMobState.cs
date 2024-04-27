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

    public class StandardMobState_WAKING_UP : StandardMobState
    {
        public StandardMobState_WAKING_UP(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.WAKING_UP;
        }

        public override void Enter()
        {
            if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
                Console.WriteLine($"{_brain.Body} is entering WAKING_UP");

            _brain.Body?.StopMoving();
            base.Enter();
        }

        public override void Think()
        {
            _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            _brain.Think();
        }
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
            if (_brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                return;

            if (_brain.HasPatrolPath())
            {
                _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
                return;
            }

            if (!_brain.Body.IsNearSpawn)
            {
                _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
                return;
            }

            if (_brain.CheckProximityAggro())
            {
                _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                return;
            }

            if (_brain.Body.CanRoam)
            {
                _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);
                return;
            }

            base.Think();
        }
    }

    public class StandardMobState_AGGRO : StandardMobState
    {
        private const int LEAVE_WHEN_OUT_OF_COMBAT_FOR = 25000;
        private long _aggroEndTime; // Used to prevent leaving on the first think tick, due to `InCombatInLast` returning false.

        public StandardMobState_AGGRO(StandardMobBrain brain) : base(brain)
        {
            StateType = eFSMStateType.AGGRO;
        }

        public override void Enter()
        {
            if (ECS.Debug.Diagnostics.StateMachineDebugEnabled)
                Console.WriteLine($"{_brain.Body} is entering AGGRO");

            _aggroEndTime = GameLoop.GameLoopTime + LEAVE_WHEN_OUT_OF_COMBAT_FOR;
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
            if (!_brain.HasAggro || (!_brain.Body.InCombatInLast(LEAVE_WHEN_OUT_OF_COMBAT_FOR) && ServiceUtils.ShouldTick(_aggroEndTime)))
            {
                if (!_brain.Body.IsIncapacitated)
                    _brain.FSM.SetCurrentState(eFSMStateType.IDLE);

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
        private long _nextRoamingTick;
        private bool _nextRoamingTickSet;
        protected virtual short Speed => NpcMovementComponent.DEFAULT_WALK_SPEED;
        protected virtual int MinCooldown => Properties.GAMENPC_ROAM_COOLDOWN_MIN;
        protected virtual int MaxCooldown => Properties.GAMENPC_ROAM_COOLDOWN_MAX;

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
            if (_brain.CheckProximityAggro())
            {
                _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                return;
            }

            if (!_brain.Body.IsCasting && !_brain.Body.IsMoving && !_brain.Body.movementComponent.HasActiveResetHeadingAction)
            {
                if (!_nextRoamingTickSet)
                {
                    _nextRoamingTickSet = true;
                    _nextRoamingTick += Util.Random(MinCooldown, MaxCooldown) * 1000;
                }

                if (ServiceUtils.ShouldTickAdjust(ref _nextRoamingTick))
                {
                    // We're not updating `_nextRoamingTick` here because we want it to be set after the NPC stopped moving.
                    _nextRoamingTickSet = false;
                    _brain.Body.Roam(Speed);
                    _brain.Body.FireAmbientSentence(GameNPC.eAmbientTrigger.roaming, _brain.Body);
                }
            }

            _brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);
            base.Think();
        }
    }

    public class StandardMobState_RETURN_TO_SPAWN : StandardMobState
    {
        protected virtual short Speed => NpcMovementComponent.DEFAULT_WALK_SPEED;

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
            base.Enter();
        }

        public override void Think()
        {
            if (_brain.Body.IsNearSpawn)
            {
                _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                _brain.Body.TurnTo(_brain.Body.SpawnHeading);
                return;
            }

            if (!_brain.Body.IsReturningToSpawnPoint)
                _brain.Body.ReturnToSpawnPoint(Speed);

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
            if (_brain.CheckProximityAggro())
            {
                _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
                return;
            }

            // TODO: NPCs can get stuck here. Find a way to resume patrols.
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
            _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            base.Think();
        }
    }
}
