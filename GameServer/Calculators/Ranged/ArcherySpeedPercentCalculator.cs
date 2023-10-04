namespace DOL.GS.PropertyCalc;

/// <summary>
/// The Archery Speed bonus percent calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(eProperty.ArcherySpeed)]
public class ArcherySpeedPercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, eProperty property)
	{
		int archerySpeed = 0;
		archerySpeed +=  living.BaseBuffBonusCategory[(int)property] 
		                 + living.SpecBuffBonusCategory[(int)property] 
		                 - living.DebuffCategory[(int)property] 
		                 + living.BuffBonusCategory4[(int)property] 
		                 + living.AbilityBonus[(int)property] ;
		//hardcap at 10%
		//return Math.Min(10, living.ItemBonus[(int)property] - living.DebuffCategory[(int)property]);

		return archerySpeed;
	}
}