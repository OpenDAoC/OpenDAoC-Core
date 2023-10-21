using System.Collections.Generic;
using Core.GS.Realm;

namespace Core.GS.PlayerClass
{
	[PlayerClass((int)EPlayerClass.Warlock, "Warlock", "Mystic")]
	public class ClassWarlock : ClassMystic
	{
		public ClassWarlock()
			: base()
		{
			m_profession = "PlayerClass.Profession.HouseofHel";
			m_specializationMultiplier = 10;
			m_primaryStat = EStat.PIE;
			m_secondaryStat = EStat.CON;
			m_tertiaryStat = EStat.DEX;
			m_manaStat = EStat.PIE;
		}

		public override bool HasAdvancedFromBaseClass()
		{
			return true;
		}

		/// <summary>
		/// FIXME this has nothing to do here !
		/// </summary>
		/// <param name="line"></param>
		/// <param name="spell"></param>
		/// <returns></returns>
		public override bool CanChangeCastingSpeed(SpellLine line, Spell spell)
		{
			if (spell.SpellType == ESpellType.Chamber)
				return false;

			if ((line.KeyName == "Cursing"
				 || line.KeyName == "Cursing Spec"
				 || line.KeyName == "Hexing"
				 || line.KeyName == "Witchcraft")
				&& (spell.SpellType != ESpellType.ArmorFactorBuff
					&& spell.SpellType != ESpellType.Bladeturn
					&& spell.SpellType != ESpellType.ArmorAbsorptionBuff
					&& spell.SpellType != ESpellType.MatterResistDebuff
					&& spell.SpellType != ESpellType.Uninterruptable
					&& spell.SpellType != ESpellType.Powerless
					&& spell.SpellType != ESpellType.Range
					&& spell.Name != "Lesser Twisting Curse"
					&& spell.Name != "Twisting Curse"
					&& spell.Name != "Lesser Winding Curse"
					&& spell.Name != "Winding Curse"
					&& spell.Name != "Lesser Wrenching Curse"
					&& spell.Name != "Wrenching Curse"
					&& spell.Name != "Lesser Warping Curse"
					&& spell.Name != "Warping Curse"))
			{
				return false;
			}

			return true;
		}

		public override List<PlayerRace> EligibleRaces => new List<PlayerRace>()
		{
			// PlayerRace.Frostalf, PlayerRace.Kobold, PlayerRace.Norseman,
		};
	}
}