using DOL.AI;
using DOL.AI.Brain;
using DOL.GS;
using FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.AI.Brain.StandardMobBrain;

public class ControlledNPCState_WAKING_UP : StandardMobState_WAKING_UP
{
    public ControlledNPCState_WAKING_UP(FSM fsm, StandardMobBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.WAKING_UP;
    }

    public override void Think()
    {
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);

        // Load abilities on first Think cycle.
        if (!brain.checkAbility)
        {
            brain.CheckAbilities();
            brain.Body.SortSpells();
            brain.checkAbility = true;
        }

        //determine state we should be in
        if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
        }

        if(brain.AggressionState == eAggressionState.Defensive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        }

        if (brain.AggressionState == eAggressionState.Passive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
        }

        //put this here so no delay after entering initial state before next think()
        brain.Think();
    }
}

public class ControlledNPCState_DEFENSIVE : StandardMobState_IDLE
{
    public ControlledNPCState_DEFENSIVE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Think()
    {
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);

        //handle state changes
        if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
        }

        if (brain.AggressionState == eAggressionState.Passive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
        }

        GamePlayer playerowner = brain.GetPlayerOwner();

        //See if the pet is too far away, if so release it!
        if (brain.Owner is GamePlayer && brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
            (brain.Owner as GamePlayer).CommandNpcRelease();

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        //handle pet movement
        if (brain.WalkState == eWalkState.Follow && brain.Owner != null)
            brain.Follow(brain.Owner);
        if(brain.WalkState == eWalkState.GoTarget && brain.Body.TargetObject != null)
        {
            brain.Goto(brain.Body.TargetObject);
        }

        //cast defensive spells if applicable
        brain.CheckSpells(eCheckSpellType.Defensive);

    }
}

public class ControlledNPCState_AGGRO : StandardMobState_AGGRO
{
    public ControlledNPCState_AGGRO(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Exit()
    {
        _brain.ClearAggroList();
        if(_brain.Body.IsAttacking)
            _brain.Body.StopAttack();
        _brain.Body.TargetObject = null;
    }

    public override void Think()
    {
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        if (brain.AggressionState == eAggressionState.Passive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.PASSIVE);
            return;
        }

        if (brain.WalkState == eWalkState.ComeHere)
        {
            brain.FSM.SetCurrentState(eFSMStateType.IDLE);
            return;
        }
        
        brain.CheckSpells(eCheckSpellType.Offensive);

        //handle pet movement
        //dont think we need to check to follow owner on every tick, that should be done elsewhere
        // if (brain.Body.CurrentFollowTarget==null && brain.WalkState == eWalkState.Follow && brain.Owner != null)
        //     brain.Follow(brain.Owner);
        if (brain.WalkState == eWalkState.GoTarget && brain.Body.TargetObject != null)
        {
            brain.Goto(brain.Body.TargetObject);
        }

        //See if the pet is too far away, if so release it!
        if (brain.Owner is GamePlayer && brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
            (brain.Owner as GamePlayer).CommandNpcRelease();

        // if pet is in agressive mode then check aggressive spells and attacks first
        if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.CheckPlayerAggro();
            brain.CheckNPCAggro();
            brain.AttackMostWanted();
        }

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

        // Check for buffs, heals, etc, interrupting melee if not being interrupted
        // Only prevent casting if we are ordering pet to come to us or go to target
        if (brain.Owner is GameNPC || (brain.Owner is GamePlayer && brain.WalkState != eWalkState.ComeHere && brain.WalkState != eWalkState.GoTarget))
            brain.CheckSpells(eCheckSpellType.Defensive);
   
        // Always check offensive spells, or pets in melee will keep blindly melee attacking,
        //	when they should be stopping to cast offensive spells.
        if(brain.Body.CurrentSpellHandler != null) return;
        
        //return to defensive if our target(s) are dead
        if(!brain.HasAggressionTable() && brain.OrderedAttackTarget == null && brain.AggressionState != eAggressionState.Aggressive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        } else
        {
            brain.AttackMostWanted();
        }

        //if(brain.Body.TargetObject == null && !brain.Body.IsCasting)
        //{
        //    brain.FollowOwner();
        //}

    }
}

public class ControlledNPCState_PASSIVE : StandardMobState
{
    public ControlledNPCState_PASSIVE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.PASSIVE;
    }

    public override void Enter()
    {
        if (_brain.Body.castingComponent.IsCasting) { _brain.Body.StopCurrentSpellcast(); }
        base.Enter();
    }

    public override void Think()
{
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        //See if the pet is too far away, if so release it!
        if (brain.Owner is GamePlayer && brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
            (brain.Owner as GamePlayer).CommandNpcRelease();

        //handle state changes
        if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.AGGRO);
        }

        if(brain.AggressionState == eAggressionState.Defensive)
        {
            brain.FSM.SetCurrentState(eFSMStateType.IDLE);
        }

        //handle pet movement
        if (brain.WalkState == eWalkState.Follow && brain.Owner != null)
            brain.Follow(brain.Owner);
        if (brain.WalkState == eWalkState.GoTarget && brain.Body.TargetObject != null)
        {
            brain.Goto(brain.Body.TargetObject);
        }
        
        //cast defensive spells if applicable
        brain.CheckSpells(eCheckSpellType.Defensive);
    }
}
