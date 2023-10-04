using System;

namespace DOL.GS.PropertyCalc
{
	/// <summary>
	/// The Melee Speed bonus percent calculator
	///
	/// BuffBonusCategory1 is used for buffs
	/// BuffBonusCategory2 unused
	/// BuffBonusCategory3 is used for debuff
	/// BuffBonusCategory4 unused
	/// BuffBonusMultCategory1 unused
	/// </summary>
	[APropertyCalculator(eProperty.MeleeSpeed)]
	public class MeleeAttackSpeedPercentCalculator : PropertyCalculator
	{
		public override int CalcValue(GameLiving living, eProperty property)
		{
			if (living is GameNPC)
			{
				// NPC buffs effects are halved compared to debuffs, so it takes 2% debuff to mitigate 1% buff
				// See PropertyChangingSpell.ApplyNpcEffect() for details.
				int buffs = living.BaseBuffBonusCategory[property] << 1;
				int debuff = Math.Abs(living.DebuffCategory[property]);
				int specDebuff = Math.Abs(living.SpecDebuffCategory[property]);

				buffs -= specDebuff;
				if (buffs > 0)
					buffs = buffs >> 1;
				buffs -= debuff;

				return 100 - buffs;
			}

			return Math.Max(1, 100
				-living.BaseBuffBonusCategory[(int)property] // less is faster = buff
				+Math.Abs(living.DebuffCategory[(int)property]) // more is slower = debuff
				-Math.Min(10, living.ItemBonus[(int)property])) // http://www.camelotherald.com/more/1325.shtml
				- living.AbilityBonus[(int)property] ;
		}
	}
}
