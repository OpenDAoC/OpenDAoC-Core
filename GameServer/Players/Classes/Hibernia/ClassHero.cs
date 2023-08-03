using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Hero, "Hero", "Guardian", "Heroine")]
	public class ClassHero : ClassGuardian
	{
		public ClassHero()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofFocus";
			m_specializationMultiplier = 20;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.DEX;
			m_wsbase = 440;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 //PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Graoch, PlayerRace.Lurikeen, PlayerRace.Shar, PlayerRace.Sylvan,
			 PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Lurikeen, PlayerRace.Sylvan,
		};
	}
}