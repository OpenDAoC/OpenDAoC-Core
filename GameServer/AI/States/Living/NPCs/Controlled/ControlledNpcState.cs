using DOL.GS;

namespace DOL.AI.Brain;

public class ControlledNpcStateWakingUp : StandardNpcStateWakingUp
{
    public ControlledNpcStateWakingUp(ControlledNpcBrain brain) : base(brain)
    {
        StateType = EFSMStateType.WAKING_UP;
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
        if (brain.AggressionState == EAggressionState.Aggressive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);
        else if (brain.AggressionState == EAggressionState.Defensive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.IDLE);
        else if (brain.AggressionState == EAggressionState.Passive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.PASSIVE);

        // Put this here so no delay after entering initial state before next Think().
        brain.Think();
    }
}

public class ControlledNpcStateDefensive : StandardNpcStateIdle
{
    public ControlledNpcStateDefensive(ControlledNpcBrain brain) : base(brain)
    {
        StateType = EFSMStateType.IDLE;
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
        if (brain.AggressionState == EAggressionState.Aggressive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);
        else if (brain.AggressionState == EAggressionState.Passive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.PASSIVE);

        // Handle pet movement.
        if (brain.WalkState == EWalkState.Follow && brain.Owner != null)
            brain.Follow(brain.Owner);

        // Cast defensive spells if applicable.
        brain.CheckSpells(ECheckSpellType.Defensive);
    }
}

public class ControlledNpcStateAggro : StandardNpcStateAggro
{
    public ControlledNpcStateAggro(ControlledNpcBrain brain) : base(brain)
    {
        StateType = EFSMStateType.AGGRO;
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

        if (brain.AggressionState == EAggressionState.Passive)
        {
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.PASSIVE);
            return;
        }

        //brain.CheckSpells(eCheckSpellType.Offensive);

        if (brain.AggressionState == EAggressionState.Aggressive)
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
        if (brain.Owner is GameNpc || (brain.Owner is GamePlayer && !hasTarget))
        {
            if (brain.CheckSpells(ECheckSpellType.Defensive))
                return;
        }

        // Return to defensive if there's no valid target.
        if (!hasTarget && brain.AggressionState != EAggressionState.Aggressive)
        {
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.IDLE);
            return;
        }

        brain.AttackMostWanted();
    }
}

public class ControlledNpcStatePassive : StandardNpcState
{
    public ControlledNpcStatePassive(ControlledNpcBrain brain) : base(brain)
    {
        StateType = EFSMStateType.PASSIVE;
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
        if (brain.AggressionState == EAggressionState.Aggressive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.AGGRO);
        else if (brain.AggressionState == EAggressionState.Defensive)
            brain.FiniteStateMachine.SetCurrentState(EFSMStateType.IDLE);

        // Handle pet movement.
        if (brain.WalkState == EWalkState.Follow && brain.Owner != null)
            brain.Follow(brain.Owner);

        // Cast defensive spells if applicable.
        brain.CheckSpells(ECheckSpellType.Defensive);
    }
}