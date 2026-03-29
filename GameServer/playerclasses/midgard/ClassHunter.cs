using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Hunter, "Hunter", "Rogue", "Huntress")]
	public class ClassHunter : ClassMidgardRogue
	{
		private static readonly string[] AutotrainableSkills = new[] { Specs.Archery, Specs.CompositeBow };

		public ClassHunter()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofSkadi";
			m_specializationMultiplier = 20;
			m_primaryStat = eStat.DEX;
			m_secondaryStat = eStat.QUI;
			m_tertiaryStat = eStat.STR;
			m_wsbase = 380;
			m_manaStat = eStat.DEX; 
		}

		// public override IList<string> GetAutotrainableSkills()
		// {
		// 	return AutotrainableSkills;
		// }

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
			 PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Valkyn,
		};
	}
}
