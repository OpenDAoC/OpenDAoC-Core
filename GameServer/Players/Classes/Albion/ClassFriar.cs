using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Friar, "Friar", "Acolyte")]
public class ClassFriar : ClassAcolyte
{
	public ClassFriar()
		: base()
	{
		m_profession = "PlayerClass.Profession.DefendersofAlbion";
		m_specializationMultiplier = 15; //atlas reduced from 18
		m_primaryStat = EStat.PIE;
		m_secondaryStat = EStat.CON;
		m_tertiaryStat = EStat.STR;
		m_manaStat = EStat.PIE;
		m_wsbase = 380;
		m_baseHP = 720;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Briton, //atlas only briton friars should be allowed
	};
}