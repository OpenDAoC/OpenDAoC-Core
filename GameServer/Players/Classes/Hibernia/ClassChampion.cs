using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Champion, "Champion", "Guardian")]
	public class ClassChampion : ClassGuardian
	{
		public ClassChampion()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofEssence";
			m_specializationMultiplier = 20;
			m_primaryStat = EStat.STR;
			m_secondaryStat = EStat.INT;
			m_tertiaryStat = EStat.DEX;
			m_manaStat = EStat.INT; //TODO: not sure
			m_wsbase = 380;
			m_baseHP = 760;
		}

		public override EPlayerClassType ClassType
		{
			get { return EPlayerClassType.Hybrid; }
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			 //PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen, PlayerRace.Shar,
			 PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen,
		};
	}
}