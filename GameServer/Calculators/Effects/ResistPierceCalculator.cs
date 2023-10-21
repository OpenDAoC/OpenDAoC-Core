using System;

namespace Core.GS.Calculators;

/// <summary>
/// The resist pierce bonus calculator
///
/// BuffBonusCategory1 is used for buffs
/// BuffBonusCategory2 unused
/// BuffBonusCategory3 is used for debuff (never seen on live)
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// </summary>
[PropertyCalculator(EProperty.ResistPierce)]
public class ResistPierceCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property)
	{
		// cap at living.level/5
		return Math.Min(Math.Max(1,living.Level/5),
			living.BaseBuffBonusCategory[(int)property]
			- living.DebuffCategory[(int)property]
			+ living.ItemBonus[(int)property]); 
		/*
		* 
		* Test Version 1.70v Release Notes June 1, 2004
		* NEW THINGS AND BUG FIXES
		* - Spell Piercing bonuses now correctly modify resistances downward instead of upward,
		* for direct damage spells, debuffs and damage over time spells.
		* A level 50 character can not have more than 10% Spell Piercing effect up at any one time 
		* (from items or spells); any Spell Piercing over 10% is ignored 
		* (previously Spell Piercing was "capped" at 25% for a level 50 character). 
		*/
		// http://www.camelotherald.com/more/1325.shtml
	}
}