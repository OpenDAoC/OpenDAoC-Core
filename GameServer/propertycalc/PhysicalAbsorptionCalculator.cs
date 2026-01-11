namespace DOL.GS.PropertyCalc
{
    [PropertyCalculator(eProperty.PhysicalAbsorption)]
    public class PhysicalAbsorptionCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property)
        {
            // Placeholder, caps and usages unknown.
            int abilityBonus = living.AbilityBonus[property];
            int itemBonus = living.ItemBonus[property];
            int buffBonus = living.BaseBuffBonusCategory[property] + living.SpecBuffBonusCategory[property];
            int debuffMalus = living.DebuffCategory[property];
            return abilityBonus + buffBonus + itemBonus - debuffMalus;
        }
    }
}
