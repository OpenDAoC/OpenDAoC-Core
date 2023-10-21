using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Acolyte, "Acolyte", "Acolyte")]
public class ClassAcolyte : PlayerClassBase
{
	public ClassAcolyte()
		: base()
	{
		m_specializationMultiplier = 10;
		m_wsbase = 320;
		m_baseHP = 720;
		m_manaStat = EStat.PIE;
	}

	public override string GetTitle(GamePlayer player, int level)
	{
		return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

    public override GameTrainer.eChampionTrainerType ChampionTrainerType()
	{
		return GameTrainer.eChampionTrainerType.Acolyte;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return false;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		PlayerRace.Korazh, PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Highlander, PlayerRace.Inconnu,
	};
}