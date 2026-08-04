using DOL.GS;

namespace DOL.AI.Brain
{
    public class ControlledMobState_WAKING_UP : StandardMobState_WAKING_UP
    {
        private bool _abilitiesChecked;

        public ControlledMobState_WAKING_UP(ControlledMobBrain brain) : base(brain) { }

        public override void Enter()
        {
            if (_abilitiesChecked)
                return;

            ControlledMobBrain brain = _brain as ControlledMobBrain;
            brain.Body.SortSpells();
            _abilitiesChecked = true;
        }

        public override void Think()
        {
            ControlledMobBrain brain = _brain as ControlledMobBrain;

            if (brain.AggressionState is eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState is eAggressionState.Defensive)
                brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            else if (brain.AggressionState is eAggressionState.Passive)
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
        }
    }

    public class ControlledMobState_DEFENSIVE : StandardMobState_IDLE
    {
        public ControlledMobState_DEFENSIVE(ControlledMobBrain brain) : base(brain) { }

        public override void Enter()
        {
            // Don't call base since it makes pets stop moving.
        }

        public override void Think()
        {
            ControlledMobBrain brain = _brain as ControlledMobBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledMobBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            // Handle state changes.
            if (brain.AggressionState is eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState is eAggressionState.Passive)
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);

            brain.CheckAbilities();

            // Cast defensive spells if applicable.
            if (!brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                brain.ResumeWalkState();
        }
    }

    public class ControlledMobState_AGGRO : StandardMobState_AGGRO
    {
        public ControlledMobState_AGGRO(ControlledMobBrain brain) : base(brain) { }

        public override void Think()
        {
            ControlledMobBrain brain = _brain as ControlledMobBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledMobBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            // Return to passive if requested, unless confused.
            if (brain.AggressionState is eAggressionState.Passive && !brain.Body.IsConfused)
            {
                brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
                return;
            }

            if (brain.AggressionState is eAggressionState.Aggressive)
                brain.CheckProximityAggro();

            // This was added in 1.88 : https://camelotherald.fandom.com/wiki/Patch_Notes:_Version_1.88
            // Removing to conform to 1.65.
            /*if (brain.Body.TargetObject is GamePlayer playerTarget && playerTarget.IsStealthed)
            {
                brain.RemoveFromAggroList(playerTarget);
                brain.OrderedAttackTarget = null;
            }*/

            brain.AttackMostWanted();
            brain.CheckAbilities();

            if (!brain.HasAggro && brain.OrderedAttackTarget == null)
            {
                // Return to defensive if there's no valid target, unless confused.

                if (brain.AggressionState is not eAggressionState.Aggressive && !brain.Body.IsConfused)
                {
                    brain.Disengage();
                    brain.FSM.SetCurrentState(eFSMStateType.IDLE);
                    return;
                }

                // Only check defensive spells if there's no target.
                if (!brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                    brain.ResumeWalkState();
            }
        }
    }

    public class ControlledMobState_PASSIVE : StandardMobState
    {
        public override eFSMStateType StateType => eFSMStateType.PASSIVE;

        public ControlledMobState_PASSIVE(ControlledMobBrain brain) : base(brain) { }

        public override void Think()
        {
            ControlledMobBrain brain = _brain as ControlledMobBrain;
            GamePlayer playerOwner = brain.GetPlayerOwner();

            if (playerOwner != null)
            {
                // See if the pet is too far away, if so release it!
                if (brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledMobBrain.MAX_OWNER_FOLLOW_DIST))
                    playerOwner.CommandNpcRelease();
            }

            // Handle state changes.
            if (brain.AggressionState is eAggressionState.Aggressive)
                brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
            else if (brain.AggressionState is eAggressionState.Defensive)
                brain.FSM.SetCurrentState(eFSMStateType.IDLE);

            brain.CheckAbilities();

            if (!brain.CheckSpells(StandardMobBrain.eCheckSpellType.Defensive))
                brain.ResumeWalkState();

        }
    }
}
