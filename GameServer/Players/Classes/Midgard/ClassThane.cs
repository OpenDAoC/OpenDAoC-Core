using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Thane, "Thane", "Viking")]
public class ClassThane : ClassViking
{
	public ClassThane()
		: base()
	{
		m_profession = "PlayerClass.Profession.HouseofThor";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.STR;
		m_secondaryStat = EStat.PIE;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.PIE;
		//changed to increase in damage table
		m_wsbase = 360;
		m_baseHP = 720;
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Norseman, PlayerRace.Troll,
	};
}