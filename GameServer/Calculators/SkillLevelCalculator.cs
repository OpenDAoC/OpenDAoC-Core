namespace DOL.GS.PropertyCalc;

[PropertyCalculator(eProperty.Skill_First, eProperty.Skill_Last)]
public class SkillLevelCalculator : PropertyCalculator
{
	public SkillLevelCalculator() {}

	public override int CalcValue(GameLiving living, eProperty property) 
	{
//			DOLConsole.WriteSystem("calc skill prop "+property+":");
		if (living is GamePlayer) 
		{
			GamePlayer player = (GamePlayer)living;

			int itemCap = player.Level/5+1;

			int itemBonus = player.ItemBonus[(int)property];

			if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMeleeWeapon))
				itemBonus += player.ItemBonus[(int)eProperty.AllMeleeWeaponSkills];
			if (SkillBase.CheckPropertyType(property, ePropertyType.SkillMagical))
				itemBonus += player.ItemBonus[(int)eProperty.AllMagicSkills];
			if (SkillBase.CheckPropertyType(property, ePropertyType.SkillDualWield))
				itemBonus += player.ItemBonus[(int)eProperty.AllDualWieldingSkills];
			if (SkillBase.CheckPropertyType(property, ePropertyType.SkillArchery))
				itemBonus += player.ItemBonus[(int)eProperty.AllArcherySkills];

			itemBonus += player.ItemBonus[(int)eProperty.AllSkills];

			if (itemBonus > itemCap)
				itemBonus = itemCap;
			int buffs = player.BaseBuffBonusCategory[(int)property]; // one buff category just in case..

			return itemBonus + buffs + player.RealmLevel/10;
		} 
		else 
		{
			// TODO other living types
		}
		return 0;
	}
}