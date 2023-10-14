namespace DOL.GS.Spells
{
	/// <summary>
	/// Vamps magical strike 
	/// </summary>
	[SpellHandler("MagicalStrike")]
	public class VampiirMagicalStrikeSpell : DirectDamageSpell
	{
		public VampiirMagicalStrikeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }

		public override int CalculateSpellResistChance(GameLiving target)
		{
			//This needs to be corrected as vampiir claws don't seem to act the same as normal damage spells
			//Same level or lower resists 0%
			//Every level above vamp level increases percent by .5%
			return target.Level <= Caster.Level ? 0 : (target.Level - Caster.Level) / 2;
			//return base.CalculateSpellResistChance(target);
		}
	}
}
