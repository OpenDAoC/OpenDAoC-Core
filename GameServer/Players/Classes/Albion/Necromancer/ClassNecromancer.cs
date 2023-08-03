using System.Collections.Generic;
using Core.GS.Players.Classes.Necromancer;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Necromancer, "Necromancer", "Disciple")]
	public class ClassNecromancer : ClassNecromancerOwner
	{
		public ClassNecromancer()
			: base()
		{
			m_profession = "PlayerClass.Profession.TempleofArawn";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.INT;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.QUI;
			m_manaStat = EStat.INT;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
		};
	}
}