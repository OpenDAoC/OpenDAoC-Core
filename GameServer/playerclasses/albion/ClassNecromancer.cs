using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[CharacterClass((int)eCharacterClass.Necromancer, "Necromancer", "Disciple")]
	public class ClassNecromancer : ClassDisciple
	{
		public ClassNecromancer()
			: base()
		{
			m_profession = "PlayerClass.Profession.TempleofArawn";
			m_specializationMultiplier = 10;
			m_primaryStat = eStat.INT;
			m_secondaryStat = eStat.DEX;
			m_tertiaryStat = eStat.QUI;
			m_manaStat = eStat.INT;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen, PlayerRace.Avalonian
		};
	}
}
