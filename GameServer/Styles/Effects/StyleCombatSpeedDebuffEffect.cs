namespace DOL.GS.Spells
{
	/// <summary>
	/// Style combat speed debuff effect spell handler
	/// </summary>
	[SpellHandler("StyleCombatSpeedDebuff")]
	public class StyleCombatSpeedDebuffEffect : CombatSpeedDebuffSpell
	{
		public override int CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		/// <summary>
		/// Calculates the effect duration in milliseconds
		/// </summary>
		/// <param name="target">The effect target</param>
		/// <param name="effectiveness">The effect effectiveness</param>
		/// <returns>The effect duration in milliseconds</returns>
		protected override int CalculateEffectDuration(GameLiving target, double effectiveness)
		{
			return Spell.Duration;
		}

		// constructor
		public StyleCombatSpeedDebuffEffect(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
