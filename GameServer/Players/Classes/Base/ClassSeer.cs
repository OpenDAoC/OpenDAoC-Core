using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Seer, "Seer", "Seer")]
	public class ClassSeer : PlayerClassBase
	{
		public ClassSeer()
			: base()
		{
			m_specializationMultiplier = 10;
			m_wsbase = 360;
			m_baseHP = 720;
			m_manaStat = EStat.PIE;
		}

		public override string GetTitle(GamePlayer player, int level)
		{
			return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
		}

		public override eClassType ClassType
		{
			get { return eClassType.Hybrid; }
		}

		public override GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.Seer;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return false;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Dwarf, PlayerRace.Frostalf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Troll,
		};
	}
}