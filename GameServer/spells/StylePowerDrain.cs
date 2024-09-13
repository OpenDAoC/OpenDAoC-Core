namespace DOL.GS.Spells
{
	/// <summary>
	/// Style combat speed debuff effect spell handler
	/// </summary>
	[SpellHandler(eSpellType.StylePowerDrain)]
	public class StylePowerDrain : DamageToPowerSpellHandler
	{
		public override double CalculateSpellResistChance(GameLiving target)
		{
			return 0;
		}
		
		public override void OnDirectEffect(GameLiving target)
        {
            base.OnDirectEffect(target);
			SendEffectAnimation(target, 0, false, 1);
        }

		// constructor
		public StylePowerDrain(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
