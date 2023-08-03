using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Savage, "Savage", "Viking")]
	public class ClassSavage : ClassViking
	{

		public ClassSavage()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofKelgor";
			m_specializationMultiplier = 15;
			m_primaryStat = EStat.DEX;
			m_secondaryStat = EStat.QUI;
			m_tertiaryStat = EStat.STR;
			m_wsbase = 400;
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
			 PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Troll, PlayerRace.Valkyn,
		};
	}
}