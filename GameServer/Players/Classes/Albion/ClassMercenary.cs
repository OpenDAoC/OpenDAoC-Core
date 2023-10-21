using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players.Races;

namespace Core.GS.Players.Classes;

[PlayerClass((int)EPlayerClass.Mercenary, "Mercenary", "Fighter")]
public class ClassMercenary : ClassFighter
{
	private static readonly string[] AutotrainableSkills = new[] { Specs.Slash, Specs.Thrust };

	public ClassMercenary()
		: base()
	{
		m_profession = "PlayerClass.Profession.GuildofShadows";
		m_specializationMultiplier = 20;
		m_primaryStat = EStat.STR;
		m_secondaryStat = EStat.DEX;
		m_tertiaryStat = EStat.CON;
		m_baseHP = 880;
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
		 //PlayerRace.Korazh, PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre, PlayerRace.Highlander, PlayerRace.Inconnu, PlayerRace.Saracen,
		 PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.Highlander, PlayerRace.Inconnu, PlayerRace.Saracen,
	};
}