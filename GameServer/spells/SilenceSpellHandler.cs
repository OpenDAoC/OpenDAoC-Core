using DOL.GS.Effects;

namespace DOL.GS.Spells
{
	/// <summary>
	/// Reduce range needed to cast the sepll
	/// </summary>
	[SpellHandler(eSpellType.Silence)]
	public class SilenceSpellHandler : SpellHandler
	{
		public override void OnEffectStart(GameSpellEffect effect)
		{
			base.OnEffectStart(effect);
			if(effect.Owner is GamePlayer)
			{
				effect.Owner.SilencedTime = effect.Owner.CurrentRegion.Time + CalculateEffectDuration(effect.Owner);
				effect.Owner.StopCurrentSpellcast();
				effect.Owner.StartInterruptTimer(effect.Owner.SpellInterruptDuration, AttackData.eAttackType.Spell, Caster);
			}
		}

		// constructor
		public SilenceSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) {}
	}
}
