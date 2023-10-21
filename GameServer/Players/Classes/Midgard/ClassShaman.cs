using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Shaman, "Shaman", "Seer")]
public class ClassShaman : ClassSeer
{
	public ClassShaman()
		: base()
	{
		m_profession = "PlayerClass.Profession.HouseofYmir";
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
		 PlayerRace.Kobold, PlayerRace.Troll,
	};
}