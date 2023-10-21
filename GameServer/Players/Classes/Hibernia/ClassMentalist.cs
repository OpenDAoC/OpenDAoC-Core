using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Mentalist, "Mentalist", "Magician")]
	public class ClassMentalist : ClassMagician
	{
		public ClassMentalist()
			: base()
		{
			m_profession = "PlayerClass.Profession.PathofHarmony";
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
			 //PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen, PlayerRace.Shar,
			 PlayerRace.Celt, PlayerRace.Elf, PlayerRace.Lurikeen,
		};
	}
}