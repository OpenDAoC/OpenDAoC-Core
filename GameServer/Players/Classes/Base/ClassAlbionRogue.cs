using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.AlbionRogue, "Rogue", "Rogue")]
public class ClassAlbionRogue : PlayerClassBase
{
	public ClassAlbionRogue()
		: base()
	{
		m_specializationMultiplier = 10;
		m_wsbase = 360;
		m_baseHP = 720;
	}

	public override string GetTitle(GamePlayer player, int level)
	{
		return HasAdvancedFromBaseClass() ? base.GetTitle(player, level) : base.GetTitle(player, 0);
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.PureTank; }
	}

	public override GameTrainer.EChampionTrainerType ChampionTrainerType()
	{
		return GameTrainer.EChampionTrainerType.AlbionRogue;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return false;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Briton, PlayerRace.Highlander, PlayerRace.Inconnu, PlayerRace.Saracen,
	};
}