using System;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Character Stat calculator
    /// 
    /// BuffBonusCategory1 is used for all single stat buffs
    /// BuffBonusCategory2 is used for all dual stat buffs
    /// BuffBonusCategory3 is used for all debuffs (positive values expected here)
    /// BuffBonusCategory4 is used for all other uncapped modifications
    ///                    category 4 kicks in at last
    /// BuffBonusMultCategory1 used after all buffs/debuffs
    /// </summary>
    [PropertyCalculator(eProperty.WaterSpeed)]
    public class WaterSpeedCalculator : PropertyCalculator
    {
        public WaterSpeedCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int abilityBonus = living.AbilityBonus[property];
            int itemBonus = living.ItemBonus[property];
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
            int debuffMalus = Math.Abs(living.DebuffCategory[property]);
            return abilityBonus + buffBonus + itemBonus - debuffMalus; // Uncapped since the client applies its own cap anyway.
        }
    }
}
