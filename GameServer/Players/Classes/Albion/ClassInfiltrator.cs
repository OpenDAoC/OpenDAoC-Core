using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;
using Core.GS.Skills;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Infiltrator, "Infiltrator", "Rogue")]
public class ClassInfiltrator : ClassAlbionRogue
{
	private static readonly string[] AutotrainableSkills = new[] { SpecConstants.Stealth };

	public ClassInfiltrator()
		: base()
	{
		m_profession = "PlayerClass.Profession.GuildofShadows";
		m_specializationMultiplier = 25;
		m_primaryStat = EStat.DEX;
		m_secondaryStat = EStat.QUI;
		m_tertiaryStat = EStat.STR;
		m_baseHP = 720;
	}

	public override bool CanUseLefthandedWeapon
	{
		get { return true; }
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
		 PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
	};
}