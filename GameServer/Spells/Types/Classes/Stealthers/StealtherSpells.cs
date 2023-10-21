using Core.GS.Skills;

namespace Core.GS.Spells
{
	[SpellHandler("BloodRage")]
	public class BloodRageSpell : SpellHandler
	{
		public BloodRageSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	[SpellHandler("HeightenedAwareness")]
	public class HeightenedAwarenessSpell : SpellHandler
	{
		public HeightenedAwarenessSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}

	[SpellHandler("SubtleKills")]
	public class SubtleKillsSpell : SpellHandler
	{
		public SubtleKillsSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
	}
}