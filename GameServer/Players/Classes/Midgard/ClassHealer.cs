using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Healer, "Healer", "Seer")]
public class ClassHealer : ClassSeer
{
	public ClassHealer()
		: base()
	{
		m_profession = "PlayerClass.Profession.HouseofEir";
		m_specializationMultiplier = 10;
		m_primaryStat = EStat.PIE;
		m_secondaryStat = EStat.CON;
		m_tertiaryStat = EStat.STR;
		m_manaStat = EStat.PIE;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Norseman,
	};
}