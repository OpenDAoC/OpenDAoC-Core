using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Blademaster, "Blademaster", "Guardian")]
public class ClassBlademaster : ClassGuardian
{
	public ClassBlademaster()
		: base()
	{
		m_profession = "PlayerClass.Profession.PathofHarmony";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.STR;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.CON;
		m_wsbase = 440;
	}

	public override bool CanUseLefthandedWeapon
	{
		get { return true; }
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 //PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Firbolg, PlayerRace.Graoch, PlayerRace.Shar,
		PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Firbolg,
	};
}