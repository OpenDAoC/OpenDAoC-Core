using DOL.AI.Brain;
using DOL.GS;
using FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.AI.Brain.StandardMobBrain;

public class ControlledNPCState_IDLE : StandardMobFSMState_IDLE
{
    public ControlledNPCState_IDLE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = StandardMobStateType.IDLE;
    }

    public override void Think()
    {
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);
        GamePlayer playerowner = brain.GetPlayerOwner();

        //See if the pet is too far away, if so release it!
        if (brain.Owner is GamePlayer && brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
            (brain.Owner as GamePlayer).CommandNpcRelease();

        // Load abilities on first Think cycle.
        if (!brain.checkAbility)
        {
            brain.CheckAbilities();
            brain.checkAbility = true;
        }

        //Fen: idk what the hell this update Tick does but it was in the other Think() method so I moved it here
        //should probably move it to gameloop instead of GameTimer
        long lastUpdate;
        if (!playerowner.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(brain.Body.CurrentRegionID, (ushort)brain.Body.ObjectID), out lastUpdate))
            lastUpdate = 0;

        if (playerowner != null && (GameTimer.GetTickCount() - lastUpdate) > brain.ThinkInterval)
            playerowner.Out.SendObjectUpdate(brain.Body);

        

        if(brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.FSM.SetCurrentState(StandardMobStateType.AGGRO);
        }

        if (brain.WalkState == eWalkState.Follow && brain.Owner != null)
            brain.Follow(brain.Owner);
        if(brain.WalkState == eWalkState.GoTarget && brain.Body.TargetObject != null)
        {
            brain.Goto(brain.Body.TargetObject);
        }

        base.Think();
    }
}

public class ControlledNPCState_AGGRO : StandardMobFSMState_AGGRO
{
    public ControlledNPCState_AGGRO(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = StandardMobStateType.AGGRO;
    }

    public override void Exit()
    {
        _brain.ClearAggroList();
        _brain.Body.StopAttack();
        _brain.Body.TargetObject = null;
    }

    public override void Think()
    {
        ControlledNpcBrain brain = (_brain as ControlledNpcBrain);

        if(brain.AggressionState == eAggressionState.Passive || brain.WalkState == eWalkState.ComeHere)
        {
            brain.FSM.SetCurrentState(StandardMobStateType.IDLE);
            return;
        }

        if (brain.Owner is GamePlayer && brain.IsMainPet && !brain.Body.IsWithinRadius(brain.Owner, ControlledNpcBrain.MAX_OWNER_FOLLOW_DIST))
            (brain.Owner as GamePlayer).CommandNpcRelease();

        // if pet is in agressive mode then check aggressive spells and attacks first
        if (!brain.Body.AttackState && brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.CheckPlayerAggro();
            brain.CheckNPCAggro();
            brain.AttackMostWanted();
        }

        // Check for buffs, heals, etc, interrupting melee if not being interrupted
        // Only prevent casting if we are ordering pet to come to us or go to target
        if (brain.Owner is GameNPC || (brain.Owner is GamePlayer && brain.WalkState != eWalkState.ComeHere && brain.WalkState != eWalkState.GoTarget))
            brain.CheckSpells(eCheckSpellType.Defensive);


        // Stop hunting player entering in steath
        if (brain.Body.TargetObject != null && brain.Body.TargetObject is GamePlayer)
        {
            GamePlayer player = brain.Body.TargetObject as GamePlayer;
            if (brain.Body.IsAttacking && player.IsStealthed && !brain.previousIsStealthed)
            {
                brain.FSM.SetCurrentState(StandardMobStateType.IDLE);
            }
            brain.previousIsStealthed = player.IsStealthed;
        }

        // Always check offensive spells, or pets in melee will keep blindly melee attacking,
        //	when they should be stopping to cast offensive spells.
        if (brain.IsActive && brain.AggressionState != eAggressionState.Passive)
            brain.CheckSpells(eCheckSpellType.Offensive);

        if(brain.Body.TargetObject == null && brain.HasAggressionTable())
        {
            brain.AttackMostWanted();
        }

        if (brain.OrderedAttackTarget == null)
        {
            brain.FSM.SetCurrentState(StandardMobStateType.IDLE);
        }

    }
}

public class ControlledNPCState_WAKING_UP : StandardMobFSMState_WAKING_UP
{
    public ControlledNPCState_WAKING_UP(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = StandardMobStateType.WAKING_UP;
    }

    public override void Think()
    {

    }
}
