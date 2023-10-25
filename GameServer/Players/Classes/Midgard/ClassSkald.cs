using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Skald, "Skald", "Viking")]
public class ClassSkald : ClassViking
{
	public ClassSkald()
		: base()
	{
		m_profession = "PlayerClass.Profession.HouseofBragi";
		m_specializationMultiplier = 15;
		m_primaryStat = EStat.CHR;
		m_secondaryStat = EStat.STR;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.CHR;
		m_wsbase = 380;
		m_baseHP = 760;
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override ushort MaxPulsingSpells
	{
		get { return 2; }
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Troll,
	};
}