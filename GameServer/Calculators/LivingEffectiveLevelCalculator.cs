using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS.Calculators;

/// <summary>
/// The Living Effective Level calculator
/// 
/// BuffBonusCategory1 is used for buffs, uncapped
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 unused
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.LivingEffectiveLevel)]
public class LivingEffectiveLevelCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property) 
	{
		if (living is GamePlayer) 
		{
			return living.Level + living.ItemBonus[(int)property] + living.BaseBuffBonusCategory[(int)property];
		} 		
		else if (living is GameNpc) 
		{
			
			IControlledBrain brain = ((GameNpc)living).Brain as IControlledBrain;
			if (brain != null && brain.Body.effectListComponent.ContainsEffectForEffectType(EEffect.Charm))
				return brain.Owner.Level + living.ItemBonus[(int)property] + living.BaseBuffBonusCategory[(int)property];
				
			return living.Level + living.ItemBonus[(int)property] + living.BaseBuffBonusCategory[(int)property];
		}
		return 0;
	}
}