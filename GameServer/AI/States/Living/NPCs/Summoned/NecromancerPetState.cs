﻿using DOL.GS;

namespace DOL.AI.Brain;

public class NecromancerPetStateWakingUp : ControlledNpcStateWakingUp
{
    public NecromancerPetStateWakingUp(NecromancerPetBrain brain) : base(brain)
    {
        StateType = EFSMStateType.WAKING_UP;
    }

    public override void Think()
    {
        base.Think();
    }
}

public class NecromancerPetStateDefensive : ControlledNpcStateDefensive
{
    public NecromancerPetStateDefensive(NecromancerPetBrain brain) : base(brain)
    {
        StateType = EFSMStateType.IDLE;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = (NecromancerPetBrain) _brain;

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}

public class NecromancerPetStateAggro : ControlledNpcStateAggro
{
    public NecromancerPetStateAggro(NecromancerPetBrain brain) : base(brain)
    {
        StateType = EFSMStateType.AGGRO;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = (NecromancerPetBrain) _brain;

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}

public class NecromancerPetStatePassive : ControlledNpcStatePassive
{
    public NecromancerPetStatePassive(NecromancerPetBrain brain) : base(brain)
    {
        StateType = EFSMStateType.PASSIVE;
    }

    public override void Think()
    {
        NecromancerPetBrain brain = (NecromancerPetBrain) _brain;

        // If spells are queued then handle them first.
        if (brain.HasSpellsQueued())
            brain.CheckSpellQueue();

        base.Think();
    }
}