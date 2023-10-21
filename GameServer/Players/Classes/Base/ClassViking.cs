using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Viking, "Viking", "Viking")]
public class ClassViking : PlayerClassBase
{
	public ClassViking()
		: base()
	{
		m_specializationMultiplier = 10;
		m_wsbase = 440;
		m_baseHP = 880;
	}

	public override string GetTitle(GamePlayer player, int level)
	{
		return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.PureTank; }
	}

	public override GameTrainer.eChampionTrainerType ChampionTrainerType()
	{
		return GameTrainer.eChampionTrainerType.Viking;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return false;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Frostalf, PlayerRace.Kobold, PlayerRace.Deifrang, PlayerRace.Norseman, PlayerRace.Troll, PlayerRace.Valkyn,
	};
}