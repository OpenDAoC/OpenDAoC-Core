using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.SummonJuggernaut)]
	public class SummonJuggernaut : SummonSimulacrum
	{
		public SummonJuggernaut(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override void OnPetReleased(GameSummonedPet pet)
		{
			if (pet.Brain is not JuggernautBrain juggernautBrain)
				return;

			AtlasOF_JuggernautECSEffect effect = EffectListService.GetEffectOnTarget(Target, eEffect.Juggernaut) as AtlasOF_JuggernautECSEffect;
			effect?.Cancel(false);
			base.OnPetReleased(pet);
		}

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new JuggernautBrain(owner);
		}
	}
}
