using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Sorcerer, "Sorcerer", "Mage", "Sorceress")]
	public class ClassSorcerer : ClassMage
	{
		public ClassSorcerer()
			: base()
		{
			m_profession = "PlayerClass.Profession.Academy";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.INT;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.QUI;
			m_manaStat = EStat.INT;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			// PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre, PlayerRace.Inconnu, PlayerRace.Saracen,
			PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
		};
	}
}