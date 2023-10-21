using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Druid, "Druid", "Naturalist")]
	public class ClassDruid : ClassNaturalist
	{
		public ClassDruid()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofHarmony";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.EMP;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.STR;
			m_manaStat = EStat.EMP;
			m_wsbase = 320;
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