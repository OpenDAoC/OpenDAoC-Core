namespace DOL.GS.Spells
{
	[SpellHandler(eSpellType.Facilis)]
	public class FacilisSpellHandler : SpellHandler
	{
		public override bool IsOverwritable(ECSGameEffect compare)
		{
			return true;
		}
		public FacilisSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
