using System;

namespace Core.GS.Calculators;

/// <summary>
/// The Spell Range bonus percent calculator
/// 
/// BuffBonusCategory1 unused
/// BuffBonusCategory2 is used for buff (like cloudsong/zahur/etc..)
/// BuffBonusCategory3 is used for debuff
/// BuffBonusCategory4 unused
/// BuffBonusMultCategory1 unused
/// 
/// [Freya] Nidel: Item are cap to 10%, buff increase cap to 15% (10% item, 5% buff)
/// http://www.youtube.com/watch?v=XcETvw5ge3s
/// </summary>
[PropertyCalculator(EProperty.SpellRange)]
public class SpellRangePercentCalculator : PropertyCalculator
{
	public override int CalcValue(GameLiving living, EProperty property) 
	{
		int debuff = living.DebuffCategory[(int)property];
		if(debuff > 0)
		{
			//GameSpellEffect nsreduction = SpellHandler.FindEffectOnTarget(living, "NearsightReduction");
			//if(nsreduction!=null) debuff = (int)(debuff * (1.00 - nsreduction.Spell.Value * 0.01));
		}
		int buff = CalcValueFromBuffs(living, property);
	    int item = CalcValueFromItems(living, property);
	    return Math.Max(0, 100 + (buff + item) - debuff);
	}

    public override int CalcValueFromBuffs(GameLiving living, EProperty property)
    {
        return Math.Min(5, living.SpecBuffBonusCategory[(int) property]);
    }

    public override int CalcValueFromItems(GameLiving living, EProperty property)
    {
        return Math.Min(10, living.ItemBonus[(int)property]);
    }
}