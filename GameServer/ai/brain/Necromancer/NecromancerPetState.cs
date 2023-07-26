using DOL.GS;

namespace DOL.AI.Brain
{
    public class NecromancerPetState_WAKING_UP : ControlledNPCState_WAKING_UP
    {
        public NecromancerPetState_WAKING_UP(NecromancerPetBrain brain) : base(brain)
        {
            StateType = eFSMStateType.WAKING_UP;
        }

        public override void Think()
        {
            base.Think();
        }
    }

    public class NecromancerPetState_DEFENSIVE : ControlledNPCState_DEFENSIVE
    {
        public NecromancerPetState_DEFENSIVE(NecromancerPetBrain brain) : base(brain)
        {
            StateType = eFSMStateType.IDLE;
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

    public class NecromancerPetState_AGGRO : ControlledNPCState_AGGRO
    {
        public NecromancerPetState_AGGRO(NecromancerPetBrain brain) : base(brain)
        {
            StateType = eFSMStateType.AGGRO;
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

    public class NecromancerPetState_PASSIVE : ControlledNPCState_PASSIVE
    {
        public NecromancerPetState_PASSIVE(NecromancerPetBrain brain) : base(brain)
        {
            StateType = eFSMStateType.PASSIVE;
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
}
