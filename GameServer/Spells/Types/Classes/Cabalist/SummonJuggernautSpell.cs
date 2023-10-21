using System;
using Core.AI.Brain;
using Core.Events;
using Core.GS.Effects;

namespace Core.GS.Spells
{
	/// <summary>
	/// Spell handler to summon a bonedancer pet.
	/// </summary>
	/// <author>IST</author>
	[SpellHandler("SummonJuggernaut")]
	public class SummonJuggernautSpell : SummonSimulacrumSpell
	{
		public SummonJuggernautSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		protected override IControlledBrain GetPetBrain(GameLiving owner)
		{
			return new JuggernautBrain(owner);
		}
		
		protected override void OnNpcReleaseCommand(CoreEvent e, object sender, EventArgs arguments)
		{
			if (e != GameLivingEvent.PetReleased || sender is not GameNpc gameNpc)
				return;

			if (gameNpc.Brain is not JuggernautBrain juggernautBrain)
				return;

			var player = juggernautBrain.Owner as GamePlayer;

			if (player == null)
				return;

			OfRaJuggernautEcsEffect effect = (OfRaJuggernautEcsEffect)EffectListService.GetEffectOnTarget(player, EEffect.Juggernaut);
			effect?.Cancel(false);

			base.OnNpcReleaseCommand(e, sender, arguments);
		}
	}
}
