using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The Living Effective Level calculator
    /// 
    /// BuffBonusCategory1 is used for buffs, uncapped
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// </summary>
    [PropertyCalculator(eProperty.LivingEffectiveLevel)]
    public class LivingEffectiveLevelCalculator : PropertyCalculator
    {
        public override int CalcValue(GameLiving living, eProperty property) 
        {
            int level;
            int bonus = living.ItemBonus[(int) property] + living.BaseBuffBonusCategory[(int) property];

            // Summoned pets use their owner's level.
            if (living is GameSummonedPet summonedPet && summonedPet.Brain is IControlledBrain brain)
                level = brain.Owner.Level;
            else
                level = living.Level;

            return level + bonus;
        }
    }
}
