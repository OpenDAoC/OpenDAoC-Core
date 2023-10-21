using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Cabalist, "Cabalist", "Mage")]
	public class ClassCabalist : ClassMage
	{
		public ClassCabalist()
			: base()
		{
			m_profession = "PlayerClass.Profession.GuildofShadows";
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