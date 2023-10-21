using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Wizard, "Wizard", "Elementalist")]
	public class ClassWizard : ClassElementalist
	{
		public ClassWizard()
			: base()
		{
			m_profession = "PlayerClass.Profession.Academy";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.INT;
			m_secondaryStat = EStat.DEX;
			m_tertiaryStat = EStat.QUI;
			m_manaStat = EStat.INT;
			m_wsbase = 240; // yes, lower that for other casters for some reason
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			// PlayerRace.Avalonian, PlayerRace.Briton, PlayerRace.HalfOgre,
			PlayerRace.Avalonian, PlayerRace.Briton,
		};
	}
}