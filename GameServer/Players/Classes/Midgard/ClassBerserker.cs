using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Berserker, "Berserker", "Viking")]
	public class ClassBerserker : ClassViking
	{
		public ClassBerserker()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofModi";
			m_specializationMultiplier = 20;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.CON;
			m_wsbase = 440;
		}

		public override bool CanUseLefthandedWeapon
		{
			get { return true; }
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Dwarf, PlayerRace.Norseman, PlayerRace.Troll, PlayerRace.Valkyn,
		};
	}
}