using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.MaulerAlb, "Mauler", "Fighter")]
	public class ClassMaulerAlb : ClassFighter
	{
		public ClassMaulerAlb()
			: base()
		{
			m_profession = "PlayerClass.Profession.TempleofIronFist";
			m_specializationMultiplier = 15;
			m_wsbase = 440;
			m_baseHP = 600;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.QUI;
            m_manaStat = EStat.STR;
		}

		public override bool CanUseLefthandedWeapon
		{
			get { return true; }
		}

		public override EPlayerClassType ClassType
		{
			get { return EPlayerClassType.Hybrid; }
		}

		public override GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.Fighter;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			// PlayerRace.Korazh, PlayerRace.Briton, PlayerRace.Inconnu,
		};
	}
}