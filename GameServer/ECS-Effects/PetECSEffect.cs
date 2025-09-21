using DOL.GS.Spells;

namespace DOL.GS
{
    public class PetECSGameEffect : ECSGameSpellEffect
    {
        public PetECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStopEffect()
        {
            // `GameObject.Die` shouldn't be called here, since it messes up with Necromancer HP.

            // Don't use the entity that casted the spell here.
            // For Bonedancers, the spell is cast by the player, but the pet count is on the commander pet.
            if (Owner is GameSummonedPet summonedPet)
                summonedPet.Owner.UpdatePetCount(summonedPet, false);

            (SpellHandler as SummonSpellHandler)?.OnPetReleased(); // Should be done before setting health to 0.
            Owner.Health = 0; // To send proper remove packet.
            Owner.Delete();
        }
    }
}
