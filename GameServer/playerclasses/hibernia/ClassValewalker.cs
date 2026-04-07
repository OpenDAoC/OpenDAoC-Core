using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Valewalker, "Valewalker", "Forester")]
	public class ClassValewalker : ClassForester
	{
		public ClassValewalker()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofAffinity";
			m_specializationMultiplier = 15;
			m_primaryStat = eStat.STR;
			m_secondaryStat = eStat.INT;
			m_tertiaryStat = eStat.CON;
			m_manaStat = eStat.INT;
			m_wsbase = 420;
			m_baseHP = 720;
		}

		// It's suspected that Valewalker was originally (until early ToA at least) bugged and was treated as a focus caster,
		// because the current power pool formula appears to give them too much power when compared to what happens on videos.
		public override bool IsFocusCaster => false;

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
