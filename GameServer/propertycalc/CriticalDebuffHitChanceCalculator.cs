using System;
using DOL.AI.Brain;

namespace DOL.GS.PropertyCalc
{
    /// <summary>
    /// The critical hit chance calculator. Returns 0 .. 100 chance.
    /// 
    /// BuffBonusCategory1 unused
    /// BuffBonusCategory2 unused
    /// BuffBonusCategory3 unused
    /// BuffBonusCategory4 unused
    /// BuffBonusMultCategory1 unused
    /// AbilityBonus used
    /// </summary>
    [PropertyCalculator(eProperty.CriticalDebuffHitChance)]
    public class CriticalDebuffHitChanceCalculator : PropertyCalculator
    {
        public CriticalDebuffHitChanceCalculator() { }

        public override int CalcValue(GameLiving living, eProperty property)
        {
            int chance = living.AbilityBonus[(int) property];

            if (living is NecromancerPet necroPet && necroPet.Brain is IControlledBrain necroBrain && necroBrain.GetPlayerOwner() is GamePlayer playerOwner)
                chance += playerOwner.GetAbility<RealmAbilities.AtlasOF_WildArcanaAbility>()?.Amount ?? 0;

            return Math.Min(chance, 50);
        }
    }
}
