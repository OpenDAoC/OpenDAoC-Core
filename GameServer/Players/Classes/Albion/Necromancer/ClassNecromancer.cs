using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Necromancer, "Necromancer", "Disciple")]
public class ClassNecromancer : ClassNecromancerBase
{
	public ClassNecromancer()
		: base()
	{
		m_profession = "PlayerClass.Profession.TempleofArawn";
		m_specializationMultiplier = 10;
		m_primaryStat = EStat.INT;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.QUI;
		m_manaStat = EStat.INT;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
	};
}