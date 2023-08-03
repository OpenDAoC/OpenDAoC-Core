using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Valewalker, "Valewalker", "Forester")]
	public class ClassValewalker : ClassForester
	{
		public ClassValewalker()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofAffinity";
			m_specializationMultiplier = 15;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.INT;
			m_tertiaryStat = EStat.CON;
			m_manaStat = EStat.INT;
			m_wsbase = 420;
			m_baseHP = 720;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Sylvan,
		};
	}
}
