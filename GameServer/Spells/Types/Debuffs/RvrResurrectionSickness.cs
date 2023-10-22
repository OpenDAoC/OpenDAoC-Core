using Core.GS.Skills;

namespace Core.GS.Spells;

/// <summary>
/// RvR Resurrection Illness Handler
/// </summary>
[SpellHandler("RvrResurrectionIllness")]
public class RvrResurrectionSickness : PveResurrectionSickness
{
	// constructor
	public RvrResurrectionSickness(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
	{
	}
}