using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.MaulerMid, "Mauler", "Viking")]
public class ClassMaulerMid : ClassViking
{
	public ClassMaulerMid()
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
		return GameTrainer.eChampionTrainerType.Viking;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		// PlayerRace.Kobold, PlayerRace.Deifrang, PlayerRace.Norseman,
	};
}