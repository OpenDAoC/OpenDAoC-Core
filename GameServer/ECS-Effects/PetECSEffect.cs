using DOL.GS.Spells;

namespace DOL.GS
{
    public class PetECSGameEffect : ECSGameSpellEffect
    {
        public PetECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStopEffect()
        {
            SpellHandler.Caster.UpdatePetCount(false);
            (SpellHandler as SummonSpellHandler)?.OnPetReleased(Owner as GameSummonedPet); // Should be done before setting health to 0.
            Owner.Die(null);
        }
    }
}
