using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Animist, "Animist", "Forester")]
public class ClassAnimist : ClassAnimistBase
{
	public ClassAnimist()
		: base()
	{
		m_specializationMultiplier = 10;
		m_wsbase = 280;
		m_baseHP = 560;
		m_manaStat = EStat.INT;

		m_profession = "PlayerClass.Profession.PathofAffinity";
		m_primaryStat = EStat.INT;
		m_secondaryStat = EStat.CON;
		m_tertiaryStat = EStat.DEX;
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.ListCaster; }
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Celt, PlayerRace.Firbolg, PlayerRace.Sylvan,
	};
}