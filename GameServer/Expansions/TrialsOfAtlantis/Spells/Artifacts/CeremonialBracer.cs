using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("CeremonialBracerMezz")]
public class CeremonialBracerMezSpellHandler : SpellHandler
{
	public CeremonialBracerMezSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}
[SpellHandler("CeremonialBracerStun")]
public class CeremonialBracerStunSpellHandler : SpellHandler
{
	public CeremonialBracerStunSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) {}
}