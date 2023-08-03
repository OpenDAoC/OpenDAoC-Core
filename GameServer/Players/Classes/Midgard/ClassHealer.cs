using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Healer, "Healer", "Seer")]
	public class ClassHealer : ClassSeer
	{
		public ClassHealer()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofEir";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.PIE;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.STR;
			m_manaStat = EStat.PIE;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Dwarf, PlayerRace.Norseman,
		};
	}
}