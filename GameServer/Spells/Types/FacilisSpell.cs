using Core.GS.ECS;

namespace Core.GS.Spells
{
	[SpellHandler("Facilis")]
	public class FacilisSpell : SpellHandler
	{
		public override bool IsOverwritable(EcsGameSpellEffect compare)
		{
			return true;
		}
		public FacilisSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
	}
}
