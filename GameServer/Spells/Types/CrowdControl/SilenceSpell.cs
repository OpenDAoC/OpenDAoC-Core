using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	[SpellHandler("Silence")]
	public class SilenceSpell : SpellHandler
	{
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if(effect.Owner is GamePlayer)
			{
				effect.Owner.SilencedTime = effect.Owner.CurrentRegion.Time + CalculateEffectDuration(effect.Owner, Caster.Effectiveness);
				effect.Owner.StopCurrentSpellcast();
				effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, EAttackType.Spell, Caster);
			}
		}

		// constructor
		public SilenceSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}
