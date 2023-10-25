using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Naturalist, "Naturalist", "Naturalist")]
public class ClassNaturalist : PlayerClassBase
{
	public ClassNaturalist()
		: base()
	{
		m_specializationMultiplier = 10;
		m_wsbase = 360;
		m_baseHP = 720;
		m_manaStat = EStat.EMP;
	}

	public override string GetTitle(GamePlayer player, int level)
	{
		return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

	public override GameTrainer.EChampionTrainerType ChampionTrainerType()
	{
		return GameTrainer.EChampionTrainerType.Naturalist;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return false;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Sylvan, PlayerRace.Graoch
	};
}