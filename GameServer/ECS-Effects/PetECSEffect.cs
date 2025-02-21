using DOL.GS.Spells;

namespace DOL.GS
{
    public class PetECSGameEffect : ECSGameSpellEffect
    {
        public PetECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStopEffect()
        {
            // `GameObject.Die` shouldn't be called here, since it messes up with Necromancer HP.
            SpellHandler.Caster.UpdatePetCount(false);
            (SpellHandler as SummonSpellHandler)?.OnPetReleased(Owner as GameSummonedPet); // Should be done before setting health to 0.
            Owner.Health = 0; // To send proper remove packet.
            Owner.Delete();
        }
    }
}
