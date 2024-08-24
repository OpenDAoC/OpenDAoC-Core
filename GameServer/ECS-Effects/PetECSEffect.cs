using DOL.GS.Spells;

namespace DOL.GS
{
    public class PetECSGameEffect : ECSGameSpellEffect
    {
        public PetECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStopEffect()
        {
            SpellHandler.Caster.UpdatePetCount(false);
            Owner.Health = 0; // To send proper remove packet.
            Owner.Delete();
            (SpellHandler as SummonSpellHandler)?.OnPetReleased(Owner as GameSummonedPet);
        }
    }
}
