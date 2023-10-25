using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Players;

namespace Core.GS.Players;

[PlayerClass((int)EPlayerClass.Enchanter, "Enchanter", "Magician", "Enchantress")]
public class ClassEnchanter : ClassMagician
{
	public ClassEnchanter()
		: base()
	{
		m_profession = "PlayerClass.Profession.PathofEssence";
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
		 PlayerRace.Elf, PlayerRace.Lurikeen,
	};
}