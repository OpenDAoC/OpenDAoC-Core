using System.Collections.Generic;
using Core.GS.Players.Classes.Bonedancer;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Bonedancer, "Bonedancer", "Mystic")]
	public class ClassBonedancer : ClassBonedancerOwner
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

		public override eClassType ClassType
		{
			get { return eClassType.ListCaster; }
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