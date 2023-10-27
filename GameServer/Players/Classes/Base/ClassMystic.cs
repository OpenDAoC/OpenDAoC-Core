using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Mystic, "Mystic", "Mystic")]
public class ClassMystic : PlayerClassBase
{
	public ClassMystic()
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

	public override GameTrainer.EChampionTrainerType ChampionTrainerType()
	{
		return GameTrainer.EChampionTrainerType.Mystic;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return false;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Frostalf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Troll, PlayerRace.Valkyn,
	};
}