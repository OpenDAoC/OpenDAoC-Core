using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Warrior, "Warrior", "Viking")]
	public class ClassWarrior : ClassViking
	{
		private static readonly string[] AutotrainableSkills = new[] { Specs.Axe, Specs.Hammer, Specs.Sword };

		public ClassWarrior()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofTyr";
			m_specializationMultiplier = 20;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.DEX;
			m_wsbase = 460;
		}

		public override IList<string> GetAutotrainableSkills()
		{
			return AutotrainableSkills;
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