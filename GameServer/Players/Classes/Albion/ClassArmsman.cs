using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Armsman, "Armsman", "Fighter", "Armswoman")]
	public class ClassArmsman : ClassFighter
	{
		private static readonly string[] AutotrainableSkills = new[] { Specs.Slash, Specs.Thrust };

		public ClassArmsman()
			: base()
		{
			m_profession = "PlayerClass.Profession.DefendersofAlbion";
			m_specializationMultiplier = 20;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.DEX;
			m_baseHP = 880;
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
			 //PlayerRace.Korazh, PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre, PlayerRace.Highlander, PlayerRace.Inconnu, PlayerRace.Saracen,
			 PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Highlander, PlayerRace.Inconnu, PlayerRace.Saracen,
		};
	}
}