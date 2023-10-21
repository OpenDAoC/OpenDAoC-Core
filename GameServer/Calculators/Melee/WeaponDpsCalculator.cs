namespace Core.GS.PropertyCalc;

/// <summary>
/// BuffBonusCategory1 is used for all single stat buffs
/// BuffBonusCategory2 is used for all dual stat buffs
/// BuffBonusCategory3 is used for all debuffs (positive values expected here)
/// BuffBonusCategory4 is used for all other uncapped modifications
///                    category 4 kicks in at last
/// BuffBonusMultCategory1 used after all buffs/debuffs
/// </summary>
[PropertyCalculator(EProperty.DPS)]
public class WeaponDpsCalculator : PropertyCalculator
{
	public WeaponDpsCalculator() {}

	public override int CalcValue(GameLiving living, EProperty property)
	{
		return (int)(
			+living.BaseBuffBonusCategory[(int)property]
			+living.SpecBuffBonusCategory[(int)property]
			-living.DebuffCategory[(int)property]
			+living.BuffBonusCategory4[(int)property]);
	}
}