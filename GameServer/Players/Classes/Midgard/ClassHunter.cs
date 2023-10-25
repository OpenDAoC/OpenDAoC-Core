using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;
using Core.GS.Server;
using Core.GS.Skills;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Hunter, "Hunter", "MidgardRogue", "Huntress")]
public class ClassHunter : ClassMidgardRogue
{
	private static readonly string[] AutotrainableSkills = new[] { SpecConstants.Archery, SpecConstants.CompositeBow };

	public ClassHunter()
		: base()
	{
		m_profession = "PlayerClass.Profession.HouseofSkadi";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.DEX;
		m_secondaryStat = EStat.QUI;
		m_tertiaryStat = EStat.STR;
		m_wsbase = 380;
		m_manaStat = EStat.DEX; 
	}

	// public override IList<string> GetAutotrainableSkills()
	// {
	// 	return AutotrainableSkills;
	// }

	public override EPlayerClassType ClassType
	{
		get { return EPlayerClassType.Hybrid; }
	}

	/// <summary>
    /// Add all spell-lines and other things that are new when this skill is trained
    /// FIXME : this should be in database
	/// </summary>
	/// <param name="player"></param>
	/// <param name="skill"></param>
	public override void OnSkillTrained(GamePlayer player, Specialization skill)
	{
		base.OnSkillTrained(player, skill);

		switch (skill.KeyName)
		{
			case SpecConstants.CompositeBow:
				if (ServerProperty.ALLOW_OLD_ARCHERY == true)
				{
					if (skill.Level < 3)
					{
						// do nothing
					}
					else if (skill.Level < 6)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 1));
					}
					else if (skill.Level < 9)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 2));
					}
					else if (skill.Level < 12)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 3));
					}
					else if (skill.Level < 15)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 4));
					}
					else if (skill.Level < 18)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 5));
					}
					else if (skill.Level < 21)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 6));
					}
					else if (skill.Level < 24)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 7));
					}
					else if (skill.Level < 27)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 8));
					}
					else if (skill.Level >= 27)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.Critical_Shot, 9));
					}

					if (skill.Level >= 45)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.RapidFire, 2));
					}
					else if (skill.Level >= 35)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.RapidFire, 1));
					}

					if (skill.Level >= 45)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.SureShot));
					}

					if (skill.Level >= 50)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.PenetratingArrow, 3));
					}
					else if (skill.Level >= 40)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.PenetratingArrow, 2));
					}
					else if (skill.Level >= 30)
					{
						player.AddAbility(SkillBase.GetAbility(AbilityConstants.PenetratingArrow, 1));
					}
				}
				break;
		}
	}

	public override bool HasAdvancedFromBaseClass()
	{
		return true;
	}

	public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
	{
		 PlayerRace.Dwarf, PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Valkyn,
	};
}