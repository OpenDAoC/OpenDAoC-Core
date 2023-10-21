using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Shadowblade, "Shadowblade", "MidgardRogue")]
	public class ClassShadowblade : ClassMidgardRogue
	{
		private static readonly string[] AutotrainableSkills = new[] { Specs.Stealth };

		public ClassShadowblade()
			: base()
		{
			m_profession = "PlayerClass.Profession.Loki";
			m_specializationMultiplier = 22;
			m_primaryStat = EStat.DEX;
			m_secondaryStat = EStat.QUI;
			m_tertiaryStat = EStat.STR;
			m_baseHP = 760;
		}

		/// <summary>
		/// Checks whether player has ability to use lefthanded weapons
		/// </summary>
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
			 PlayerRace.Kobold, PlayerRace.Norseman, PlayerRace.Valkyn,
		};
	}
}