using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Cleric, "Cleric", "Acolyte")]
public class ClassCleric : ClassAcolyte
{
	public ClassCleric()
		: base()
	{
		m_profession = "PlayerClass.Profession.ChurchofAlbion";
		m_specializationMultiplier = 10;
		m_primaryStat = EStat.PIE;
		m_secondaryStat = EStat.CON;
		m_tertiaryStat = EStat.STR;
		m_manaStat = EStat.PIE;
		m_baseHP = 720;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Highlander,
	};
}