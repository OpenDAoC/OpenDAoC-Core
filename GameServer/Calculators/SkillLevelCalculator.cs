using System;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// The Skill Level calculator
	/// 
	/// BuffBonusCategory1 is used for buffs, uncapped
	/// BuffBonusCategory2 unused
	/// BuffBonusCategory3 unused
	/// BuffBonusCategory4 unused
	/// BuffBonusMultCategory1 unused
	/// </summary>
	[PropertyCalculator(EProperty.Skill_First, EProperty.Skill_Last)]
	public class SkillLevelCalculator : PropertyCalculator
	{
		public SkillLevelCalculator() {}

		public override int CalcValue(GameLiving living, EProperty property) 
		{
//			DOLConsole.WriteSystem("calc skill prop "+property+":");
			if (living is GamePlayer) 
			{
				GamePlayer player = (GamePlayer)living;

				int itemCap = player.Level/5+1;

				int itemBonus = player.ItemBonus[(int)property];

				if (SkillBase.CheckPropertyType(property, EPropertyType.SkillMeleeWeapon))
					itemBonus += player.ItemBonus[(int)EProperty.AllMeleeWeaponSkills];
				if (SkillBase.CheckPropertyType(property, EPropertyType.SkillMagical))
					itemBonus += player.ItemBonus[(int)EProperty.AllMagicSkills];
				if (SkillBase.CheckPropertyType(property, EPropertyType.SkillDualWield))
					itemBonus += player.ItemBonus[(int)EProperty.AllDualWieldingSkills];
				if (SkillBase.CheckPropertyType(property, EPropertyType.SkillArchery))
					itemBonus += player.ItemBonus[(int)EProperty.AllArcherySkills];

				itemBonus += player.ItemBonus[(int)EProperty.AllSkills];

				if (itemBonus > itemCap)
					itemBonus = itemCap;
				int buffs = player.BaseBuffBonusCategory[(int)property]; // one buff category just in case..

//				DOLConsole.WriteLine("item bonus="+itemBonus+"; buffs="+buffs+"; realm="+player.RealmLevel/10);
				return itemBonus + buffs + player.RealmLevel/10;
			} 
			else 
			{
				// TODO other living types
			}
			return 0;
		}
	}
}