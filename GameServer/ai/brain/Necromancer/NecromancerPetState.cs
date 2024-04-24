using DOL.GS;

namespace DOL.AI.Brain
{
    public class NecromancerPetState_DEFENSIVE : ControlledMobState_DEFENSIVE
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

    public class NecromancerPetState_AGGRO : ControlledMobState_AGGRO
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

    public class NecromancerPetState_PASSIVE : ControlledMobState_PASSIVE
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
