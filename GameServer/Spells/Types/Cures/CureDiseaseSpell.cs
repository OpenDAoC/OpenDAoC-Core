using System.Collections.Generic;
using Core.GS.Skills;

namespace Core.GS.Spells
{
	/// <summary>
	/// 
	/// </summary>
	[SpellHandler("CureDisease")]
	public class CureDiseaseSpell : RemoveSpellEffectHandler
	{
		// constructor
		public CureDiseaseSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
		{
			// RR4: now it's a list
			m_spellTypesToRemove = new List<string>();
			m_spellTypesToRemove.Add("Disease");
		}
	}
}