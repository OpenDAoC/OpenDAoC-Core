using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS.AI;

public class NecromancerPetStateWakingUp : ControlledNpcStateWakingUp
{
    public NecromancerPetStateWakingUp(NecromancerPetBrain brain) : base(brain)
    {
        StateType = EFsmStateType.WAKING_UP;
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
        StateType = EFsmStateType.IDLE;
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
        StateType = EFsmStateType.AGGRO;
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
        StateType = EFsmStateType.PASSIVE;
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