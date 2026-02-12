using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Thane, "Thane", "Viking")]
	public class ClassThane : ClassViking
	{
		public ClassThane()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofThor";
			m_specializationMultiplier = 20;
			m_primaryStat = eStat.STR;
			m_secondaryStat = eStat.PIE;
			m_tertiaryStat = eStat.CON;
			m_manaStat = eStat.PIE;
			m_wsbase = 360;
			m_baseHP = 720;
		}

		public override eClassType ClassType
		{
			get { return eClassType.Hybrid; }
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Dwarf, PlayerRace.Norseman, PlayerRace.Troll,
		};
	}
}
