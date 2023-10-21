using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Nightshade, "Nightshade", "Stalker")]
public class ClassNightshade : ClassStalker
{
	private static readonly string[] AutotrainableSkills = new[] { Specs.Stealth };

	public ClassNightshade()
		: base()
	{
		m_profession = "PlayerClass.Profession.PathofEssence";
		m_specializationMultiplier = 22;
		m_primaryStat = EStat.DEX;
		m_secondaryStat = EStat.QUI;
		m_tertiaryStat = EStat.STR;
		m_manaStat = EStat.DEX;
	}

	public override bool CanUseLefthandedWeapon
	{
		get { return true; }
	}

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

	public override IList<string> GetAutotrainableSkills()
	{
		return AutotrainableSkills;
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Elf, PlayerRace.Lurikeen,
	};
}