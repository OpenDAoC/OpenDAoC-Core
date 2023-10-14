using System;

namespace DOL.GS.Spells
{
	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class SpellHandlerAttribute : Attribute
	{
		string m_type;

		public SpellHandlerAttribute(string spellType) {
			m_type = spellType;
		}

		/// <summary>
		/// Spell type name of the denoted handler
		/// </summary>
		public string SpellType {
			get { return m_type; }
		}
	}
}
