using System.Collections.Generic;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("CureMezz")]
public class CureMezSpell : RemoveSpellEffectHandler
{
	// constructor
	public CureMezSpell(GameLiving caster, Spell spell, SpellLine line)
		: base(caster, spell, line)
	{
		// RR4: now it's a list
		m_spellTypesToRemove = new List<string>();
		m_spellTypesToRemove.Add("Mesmerize");
	} 
}