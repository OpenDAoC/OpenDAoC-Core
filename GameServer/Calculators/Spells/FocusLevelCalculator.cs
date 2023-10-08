using System;

namespace DOL.GS.PropertyCalc;

/// <summary>
/// The Focus Level calculator
///
/// BuffBonusCategory1 is used for buffs, uncapped
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.Focus_Darkness, EProperty.Focus_Matter)]
[PropertyCalculator(EProperty.Focus_Mind, EProperty.Focus_Arboreal)]
[PropertyCalculator(EProperty.Focus_EtherealShriek, EProperty.Focus_Witchcraft)]
public class FocusLevelCalculator : PropertyCalculator
{
	public FocusLevelCalculator() { }

	public override int CalcValue(GameLiving living, EProperty property)
	{
		if (living is GamePlayer)
		{
			int itemBonus = living.ItemBonus[(int)property];
			int focusLevel = living.BaseBuffBonusCategory[(int)property];
			if (SkillBase.CheckPropertyType(property, EPropertyType.Focus)
			 && ((GamePlayer)living).CharacterClass.ClassType == eClassType.ListCaster)
			{
				focusLevel += living.BaseBuffBonusCategory[(int)EProperty.AllFocusLevels];
				itemBonus = Math.Max(itemBonus, living.ItemBonus[(int)EProperty.AllFocusLevels]);
			}
			return focusLevel + Math.Min(50, itemBonus);
		}
		else
		{
			// TODO other living types
		}
		return 0;
	}
}