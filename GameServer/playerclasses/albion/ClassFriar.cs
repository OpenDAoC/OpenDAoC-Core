
using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Friar, "Friar", "Acolyte")]
	public class ClassFriar : ClassAcolyte
	{
		public ClassFriar()
			: base()
		{
			m_profession = "PlayerClass.Profession.DefendersofAlbion";
			m_specializationMultiplier = 15;
			m_primaryStat = eStat.PIE;
			m_secondaryStat = eStat.CON;
			m_tertiaryStat = eStat.STR;
			m_manaStat = eStat.PIE;
			// Damage table is unknown.
			// Uthgard had 360 (from DoL).
			// Atlas had 380 (1.65 compliance according to commit e3d73fa32c7ebe3cc3937db5c4b7a6e39208c792)
			// Phoenix had 400 (staff), but was buffed to 420.
			// Live (late 2025) has 400.
			m_wsbase = 380;
			m_baseHP = 720;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Briton, //atlas only briton friars should be allowed
		};
	}
}
