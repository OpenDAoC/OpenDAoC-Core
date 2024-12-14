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
            GameNPC body = _brain.Body;

            if (body != null)
            {
                body.StopMoving();
                body.StopAttack();
                body.StopCurrentSpellcast();
                body.TargetObject = null;
            }

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
            _brain.Body.StopMoving();
            _brain.NextThinkTick -= _brain.ThinkInterval; // Don't stay in IDLE for a full think cycle.
            base.Enter();
        }

        public override void Think()
        {
            if (_brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                return;

            if (_brain.HasPatrolPath())
                _brain.FSM.SetCurrentState(eFSMStateType.PATROLLING);
            else if (!_brain.Body.IsNearSpawn)
                _brain.FSM.SetCurrentState(eFSMStateType.RETURN_TO_SPAWN);
            else if (_brain.CheckProximityAggro())
                _brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (_brain.Body.CanRoam)
                _brain.FSM.SetCurrentState(eFSMStateType.ROAMING);

            if (_brain.FSM.GetCurrentState() != this)
                _brain.NextThinkTick -= _brain.ThinkInterval; // Don't stay in IDLE for a full think cycle.
            else
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
            _brain.Body.Flags &= ~GameNPC.eFlags.STEALTH;
            _aggroEndTime = GameLoop.GameLoopTime + LEAVE_WHEN_OUT_OF_COMBAT_FOR;
            base.Enter();
        }

        public override void Exit()
        {
            if (_brain.Body.attackComponent.AttackState)
                _brain.Body.StopAttack();

            // Don't stealth NPCs on death to prevent their corpse from immediately disappearing.
            if (_brain.Body.WasStealthed && _brain.Body.IsAlive)
                _brain.Body.Flags |= GameNPC.eFlags.STEALTH;

            _brain.Body.TargetObject = null;
            base.Exit();
        }

        public override void Think()
        {
            if (_brain.Body.IsCrowdControlled || EffectListService.GetSpellEffectOnTarget(_brain.Body, eEffect.MovementSpeedDebuff)?.SpellHandler.Spell.Value == 99)
                _aggroEndTime = GameLoop.GameLoopTime + LEAVE_WHEN_OUT_OF_COMBAT_FOR;
            else if (!_brain.Body.InCombatInLast(LEAVE_WHEN_OUT_OF_COMBAT_FOR) && ServiceUtils.ShouldTick(_aggroEndTime))
            {
                _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                return;
            }

            _brain.AttackMostWanted();

            if (!_brain.HasAggro)
            {
                _brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                return;
            }

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
}
