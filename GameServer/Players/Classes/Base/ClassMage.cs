using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Mage, "Mage", "Mage")]
	public class ClassMage : PlayerClassBase
	{
		public ClassMage()
			: base()
		{
			m_specializationMultiplier = 10;
			m_wsbase = 280;
			m_baseHP = 560;
			m_manaStat = EStat.INT;
		}

		public override string GetTitle(GamePlayer player, int level)
		{
			return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
		}

		public override eClassType ClassType
		{
			get { return eClassType.ListCaster; }
		}

		public override GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.Mage;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return false;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre, PlayerRace.Inconnu, PlayerRace.Saracen,
		};
	}
}