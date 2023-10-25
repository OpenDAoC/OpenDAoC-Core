using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;
using Core.GS.Skills;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Reaver, "Reaver", "Fighter")]
public class ClassReaver : ClassFighter
{
	private static readonly string[] AutotrainableSkills = new[] { SpecConstants.Slash, SpecConstants.Flexible };

	public ClassReaver()
		: base()
	{
		m_profession = "PlayerClass.Profession.TempleofArawn";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.STR;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.PIE;
		m_manaStat = EStat.PIE;
		m_wsbase = 380;
		m_baseHP = 760;
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
		PlayerRace.Briton, PlayerRace.Inconnu, PlayerRace.Saracen,
	};
}