using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Magician, "Magician", "Magician")]
	public class ClassMagician : PlayerClassBase
	{
		public ClassMagician()
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

		public override EPlayerClassType ClassType
		{
			get { return EPlayerClassType.ListCaster; }
		}

		public override GameTrainer.eChampionTrainerType ChampionTrainerType()
		{
			return GameTrainer.eChampionTrainerType.Magician;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return false;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen, PlayerRace.Shar,
		};
	}
}
