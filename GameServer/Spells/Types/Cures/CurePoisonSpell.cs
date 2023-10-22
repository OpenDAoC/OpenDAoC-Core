using System.Collections.Generic;
using Core.GS.Skills;

namespace Core.GS.Spells;

[SpellHandler("CurePoison")]
public class CurePoisonSpell : RemoveSpellEffectHandler
{
	// constructor
	public CurePoisonSpell(GameLiving caster, Spell spell, SpellLine line)
		: base(caster, spell, line)
	{
		// RR4: now it's a list
		m_spellTypesToRemove = new List<string>();
		m_spellTypesToRemove.Add("DamageOverTime");
        m_spellTypesToRemove.Add("StyleBleeding");
	} 
}