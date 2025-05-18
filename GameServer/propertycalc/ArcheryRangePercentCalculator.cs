using System;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// The Archery Range bonus percent calculator
	///
	/// BuffBonusCategory1 unused
	/// BuffBonusCategory2 unused
	/// BuffBonusCategory3 is used for debuff
	/// BuffBonusCategory4 unused
	/// BuffBonusMultCategory1 unused
	/// </summary>
	[PropertyCalculator(eProperty.ArcheryRange)]
	public class ArcheryRangePercentCalculator : PropertyCalculator
	{
		public override int CalcValue(GameLiving living, eProperty property)
		{
			int debuff = living.DebuffCategory[property];
			if(debuff > 0)
			{
				//GameSpellEffect nsreduction = SpellHandler.FindEffectOnTarget(living, "NearsightReduction");
				//if(nsreduction!=null) debuff = (int)(debuff * (1.00 - nsreduction.Spell.Value * 0.01));
			}
			
			int item = Math.Max(0, 100
				- debuff
				+ Math.Min(10, living.ItemBonus[property]));// http://www.camelotherald.com/more/1325.shtml

			int ra = 0;
			if (living.rangeAttackComponent.RangedAttackType == eRangedAttackType.Long)
			{
				ra = 50;
				TrueShotECSGameEffect effect = EffectListService.GetAbilityEffectOnTarget(living, eEffect.TrueShot) as TrueShotECSGameEffect;
				effect?.Stop(false);
			}

			return item + ra;
		}
	}
}
