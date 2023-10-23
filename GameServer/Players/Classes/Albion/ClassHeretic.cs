using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Heretic, "Heretic", "Acolyte")]
public class ClassHeretic : ClassAcolyte
{
	public ClassHeretic()
		: base()
	{
		m_profession = "PlayerClass.Profession.TempleofArawn";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.PIE;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.CON;
		m_manaStat = EStat.PIE;
		m_wsbase = 360;
		m_baseHP = 720;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 //PlayerRace.Korazh, PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Inconnu,
	};
}