using System.Collections.Generic;

namespace DOL.GS.Spells
{
	/// <summary>
	///
	/// </summary>
	[SpellHandler("CureAll")]
	public class CureAllSpell : RemoveSpellEffectHandler
	{
		// constructor
		public CureAllSpell(GameLiving caster, Spell spell, SpellLine line)
			: base(caster, spell, line)
		{
			m_spellTypesToRemove = new List<string>();
			m_spellTypesToRemove.Add("DamageOverTime");
			m_spellTypesToRemove.Add("Nearsight");
            m_spellTypesToRemove.Add("Silence");
			m_spellTypesToRemove.Add("Disease");
            m_spellTypesToRemove.Add("StyleBleeding");
		}
	}
}