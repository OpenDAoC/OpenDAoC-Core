using System.Collections.Generic;

namespace Core.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
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
}
