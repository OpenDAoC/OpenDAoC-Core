using DOL.GS;

namespace DOL.AI.Brain
{
    public class ControlledNPCState_WAKING_UP : StandardMobState_WAKING_UP
    {
        public ControlledNPCState_WAKING_UP(ControlledNpcBrain brain) : base(brain)
        {
            StateType = eFSMStateType.WAKING_UP;
        }

        public override void Think()
        {
            ControlledNpcBrain brain = _brain as ControlledNpcBrain;

            // Load abilities on first Think() cycle.
            if (!brain.checkAbility)
            {
                brain.CheckAbilities();
                brain.Body.SortSpells();
                brain.checkAbility = true;
            }

            // Determine state we should be in.
            if (brain.AggressionState == eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState == eAggressionState.Defensive)
                brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            else if (brain.AggressionState == eAggressionState.Passive)
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);

            // Put this here so no delay after entering initial state before next Think().
            brain.Think();
        }
    }

    public class ControlledNPCState_DEFENSIVE : StandardMobState_IDLE
    {
        public ControlledNPCState_DEFENSIVE(ControlledNpcBrain brain) : base(brain)
        {
            StateType = eFSMStateType.IDLE;
        }

        public override void Think()
        {
            ControlledNpcBrain brain = _brain as ControlledNpcBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            // Handle state changes.
            if (brain.AggressionState == eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState == eAggressionState.Passive)
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);

            // Handle pet movement.
            if (brain.WalkState == eWalkState.Follow && brain.Owner != null)
                brain.Follow(brain.Owner);

            // Cast defensive spells if applicable.
            brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);
        }
    }

    public class ControlledNPCState_AGGRO : StandardMobState_AGGRO
    {
        public ControlledNPCState_AGGRO(ControlledNpcBrain brain) : base(brain)
        {
            StateType = eFSMStateType.AGGRO;
        }

        public override void Exit()
        {
            _brain.ClearAggroList();

            if (_brain.Body.IsAttacking)
                _brain.Body.StopAttack();

            _brain.Body.TargetObject = null;
        }

        public override void Think()
        {
            ControlledNpcBrain brain = _brain as ControlledNpcBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            if (brain.AggressionState == eAggressionState.Passive)
            {
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
                return;
            }

            //brain.CheckSpells(eCheckSpellType.Offensive);

            if (brain.AggressionState == eAggressionState.Aggressive)
                brain.CheckProximityAggro();

            /* this was added in 1.88 : https://camelotherald.fandom.com/wiki/Patch_Notes:_Version_1.88
             * removing to conform to 1.65
            // Stop hunting player entering in steath
            if (brain.Body.TargetObject != null && brain.Body.TargetObject is GamePlayer)
            {
                GamePlayer player = brain.Body.TargetObject as GamePlayer;
                if (brain.Body.IsAttacking && player.IsStealthed && !brain.previousIsStealthed)
                {
                    brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                }
                brain.previousIsStealthed = player.IsStealthed;
            }*/

            bool hasTarget = brain.HasAggro || brain.OrderedAttackTarget != null;

            // Check for buffs, heals, etc, interrupting melee if not being interrupted.
            if (!hasTarget)
            {
                if (brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                    return;

                // Return to defensive if there's no valid target.
                if (brain.AggressionState != eAggressionState.Aggressive)
                {
                    brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                    return;
                }
            }

            brain.AttackMostWanted();
        }
    }

    public class ControlledNPCState_PASSIVE : StandardMobState
    {
        public ControlledNPCState_PASSIVE(ControlledNpcBrain brain) : base(brain)
        {
            StateType = eFSMStateType.PASSIVE;
        }

        public override void Enter()
        {
            if (_brain.Body.IsCasting)
                _brain.Body.StopCurrentSpellcast();

            base.Enter();
        }

        public override void Think()
        {
            ControlledNpcBrain brain = _brain as ControlledNpcBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            // Handle state changes.
            if (brain.AggressionState == eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState == eAggressionState.Defensive)
                brain.FSM.SetCurrentState(eFSMStateType.IDLE);

            // Handle pet movement.
            if (brain.WalkState == eWalkState.Follow && brain.Owner != null)
                brain.Follow(brain.Owner);

            // Cast defensive spells if applicable.
            brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive);
        }
    }
}
