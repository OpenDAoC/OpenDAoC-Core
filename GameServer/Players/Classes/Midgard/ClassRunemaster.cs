using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Runemaster, "Runemaster", "Mystic")]
	public class ClassRunemaster : ClassMystic
	{
		public ClassRunemaster()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofOdin";
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
			 PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman,
		};
	}
}