using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Bard, "Bard", "Naturalist")]
public class ClassBard : ClassNaturalist
{
	public ClassBard()
		: base()
	{
		m_profession = "PlayerClass.Profession.PathofEssence";
		m_specializationMultiplier = 15;
		m_primaryStat = EStat.CHR;
		m_secondaryStat = EStat.EMP;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.CHR;
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
		 PlayerRace.Celt, PlayerRace.Firbolg,
	};
}