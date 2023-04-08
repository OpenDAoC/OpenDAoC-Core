using DOL.AI.Brain;
using FiniteStateMachine;

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

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}

public class NecromancerPetState_AGGRO : ControlledNPCState_AGGRO
{
    public NecromancerPetState_AGGRO(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.AGGRO;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = _brain as NecromancerPetBrain;

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}

public class NecromancerPetState_PASSIVE : ControlledNPCState_PASSIVE
{
    public NecromancerPetState_PASSIVE(FSM fsm, ControlledNpcBrain brain) : base(fsm, brain)
    {
        _id = eFSMStateType.PASSIVE;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = _brain as NecromancerPetBrain;

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}
