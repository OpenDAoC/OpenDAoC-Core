using System.Collections.Generic;
using DOL.GS.Realm;

namespace DOL.GS.PlayerClass
{
	[PlayerClass((int)ECharacterClass.Guardian, "Guardian", "Guardian")]
	public class ClassGuardian : PlayerClassBase
	{
		public ClassGuardian()
			: base()
		{
			m_specializationMultiplier = 10;
			m_wsbase = 400;
			m_baseHP = 880;
		}

		public override string GetTitle(GamePlayer player, int level)
		{
			return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
		}

		public override eClassType ClassType
		{
			get { return eClassType.PureTank; }
		}

		public override GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.Guardian;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return false;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Firbolg, PlayerRace.Graoch, PlayerRace.Lurikeen, PlayerRace.Shar, PlayerRace.Sylvan,
		};
	}
}