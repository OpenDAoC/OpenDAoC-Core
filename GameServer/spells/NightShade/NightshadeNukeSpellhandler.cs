namespace DOL.GS.Spells
{
	[SpellHandler("NightshadeNuke")]
	public class NightshadeNuke : DirectDamageSpellHandler
	{
		public NightshadeNuke(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}
