using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Bonedancer, "Bonedancer", "Mystic")]
	public class ClassBonedancer : ClassBonedancerBase
	{
		public ClassBonedancer()
			: base()
		{
			m_specializationMultiplier = 10;
			m_wsbase = 280;
			m_baseHP = 560;
			m_manaStat = EStat.PIE;

			m_profession = "PlayerClass.Profession.HouseofBodgar";
			m_primaryStat = EStat.PIE;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.QUI;
		}

		public override EPlayerClassType ClassType
		{
			get { return EPlayerClassType.ListCaster; }
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Kobold, PlayerRace.Troll, PlayerRace.Valkyn,
		};
	}
}