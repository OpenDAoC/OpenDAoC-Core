namespace DOL.GS.Spells
{
	/// <summary>
	/// Style combat speed debuff effect spell handler
	/// </summary>
	[SpellHandler(eSpellType.StyleCombatSpeedDebuff)]
	public class StyleCombatSpeedDebuff : CombatSpeedDebuff
	{
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}

		protected override int CalculateEffectDuration(GameLiving target)
		{
			return Spell.Duration;
		}

		// constructor
		public StyleCombatSpeedDebuff(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
