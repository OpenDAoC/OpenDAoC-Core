using DOL.GS.Realm;
using System.Collections.Generic;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Paladin, "Paladin", "Fighter")]
	public class ClassPaladin : ClassFighter
	{
		private static readonly string[] AutotrainableSkills = new[] { Specs.Slash, Specs.Chants };

		public ClassPaladin()
			: base()
		{
			m_profession = "PlayerClass.Profession.ChurchofAlbion";
			m_specializationMultiplier = 20;
			m_primaryStat = eStat.CON;
			m_secondaryStat = eStat.PIE;
			m_tertiaryStat = eStat.STR;
			m_manaStat = eStat.PIE;
			m_wsbase = 380;
			m_baseHP = 760;
		}

		public override eClassType ClassType
		{
			get { return eClassType.Hybrid; }
		}

		public override IList<string> GetAutotrainableSkills()
		{
			return AutotrainableSkills;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override ushort MaxPulsingSpells
		{
			get { return 2; }
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Highlander, PlayerRace.Saracen,
		};
	}
}
