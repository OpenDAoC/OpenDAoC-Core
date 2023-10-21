using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Warden, "Warden", "Naturalist")]
public class ClassWarden : ClassNaturalist
{
	public ClassWarden()
		: base()
	{
		m_profession = "PlayerClass.Profession.PathofFocus";
		m_specializationMultiplier = 15; //18
		m_primaryStat = EStat.EMP;
		m_secondaryStat = EStat.STR;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.EMP;
		m_wsbase = 360;
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
		 //PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Graoch, PlayerRace.Sylvan,
		 PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Sylvan,
	};
}