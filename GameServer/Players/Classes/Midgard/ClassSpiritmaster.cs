using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Spiritmaster, "Spiritmaster", "Mystic")]
	public class ClassSpiritmaster : ClassMystic
	{
		public ClassSpiritmaster()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofHel";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.PIE;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.QUI;
			m_manaStat = EStat.PIE;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Kobold, PlayerRace.Norseman,
		};
	}
}