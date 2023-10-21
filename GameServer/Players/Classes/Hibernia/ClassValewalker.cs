using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Valewalker, "Valewalker", "Forester")]
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