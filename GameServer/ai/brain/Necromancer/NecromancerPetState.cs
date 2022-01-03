using DOL.AI.Brain;
using DOL.GS;
using FiniteStateMachine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DOL.AI.Brain.StandardMobBrain;

public class NecromancerPetState_WAKING_UP : ControlledNPCState_WAKING_UP
{
    public NecromancerPetState_WAKING_UP(FSM fsm, NecromancerPetBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.WAKING_UP;
    }

    public override void Think()
    {
        base.Think();
    }
}

public class NecromancerPetState_DEFENSIVE : ControlledNPCState_DEFENSIVE
{
    public NecromancerPetState_DEFENSIVE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.IDLE;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = _brain as NecromancerPetBrain;

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        if (brain.SpellsQueued)
            // if spells are queued then handle them first
            brain.CheckSpellQueue();
        else if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.CheckPlayerAggro();
            brain.CheckNPCAggro();
        }
        if (!brain.Body.IsCasting)
        {
            brain.AttackMostWanted();
        }
        
        // Do not discover stealthed players
        if (brain.Body.TargetObject != null)
        {
            if (brain.Body.TargetObject is GamePlayer)
            {
                if (brain.Body.IsAttacking && (brain.Body.TargetObject as GamePlayer).IsStealthed)
                {
                    brain.Body.StopAttack();
                    brain.FollowOwner();
                }
            }
        }
    }
}

public class NecromancerPetState_AGGRO : ControlledNPCState_AGGRO
{
    public NecromancerPetState_AGGRO(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Exit()
    {
        _brain.ClearAggroList();
        _brain.Body.StopAttack();
        _brain.Body.TargetObject = null;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = _brain as NecromancerPetBrain;

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        if (brain.SpellsQueued)
            // if spells are queued then handle them first
            brain.CheckSpellQueue();
        else if (brain.AggressionState == eAggressionState.Aggressive)
        {
            brain.CheckPlayerAggro();
            brain.CheckNPCAggro();
        }

        if (!brain.Body.IsCasting)
        {
            brain.AttackMostWanted();
        }

        // Do not discover stealthed players
        if (brain.Body.TargetObject != null)
        {
            if (brain.Body.TargetObject is GamePlayer)
            {
                if (brain.Body.IsAttacking && (brain.Body.TargetObject as GamePlayer).IsStealthed)
                {
                    brain.Body.StopAttack();
                    brain.FollowOwner();
                }
            }
        }
    }
}

public class NecromancerPetState_PASSIVE : ControlledNPCState_PASSIVE
{
    public NecromancerPetState_PASSIVE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
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
        NecromancerPetBrain brain = _brain as NecromancerPetBrain;

        brain.GetPlayerOwner().Out.SendObjectUpdate(brain.Body);

        if (brain.SpellsQueued)
            // if spells are queued then handle them first
            brain.CheckSpellQueue();
        base.Think();
    }
}
