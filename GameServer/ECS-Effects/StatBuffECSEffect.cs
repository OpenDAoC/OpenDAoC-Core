using DOL.GS.PropertyCalc;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class StatBuffECSEffect : ECSGameSpellEffect
    {
        public StatBuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges++;

            if (EffectType is eEffect.StrengthConBuff or eEffect.DexQuickBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
            }
            else if (SpellHandler.Spell.SpellType is eSpellType.BaseArmorFactorBuff or eSpellType.SpecArmorFactorBuff or eSpellType.PaladinArmorFactorBuff)
                ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, false);
            else if (SpellHandler.Spell.SpellType is eSpellType.AllMagicResistBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
            }
            else
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    if (EffectType is eEffect.EnduranceRegenBuff)
                        Effectiveness = 1;

                    if (EffectType is eEffect.MovementSpeedBuff)
                    {
                        if (!Owner.IsStealthed)
                        {
                            Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, EffectType, SpellHandler.Spell.Value / 100.0);
                            Owner.OnMaxSpeedChange();
                        }

                        if (Owner.IsStealthed)
                            EffectService.RequestDisableEffect(this);
                    }
                    else
                        ApplyBonus(Owner, eBuffBonusCategory.BaseBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
                }
            }

            // Let's not bother checking the effect type and simply attempt to start every regeneration timer instead.
            Owner.StartHealthRegeneration();
            Owner.StartEnduranceRegeneration();
            Owner.StartPowerRegeneration();

            // "You feel more dexterous!"
            // "{0} looks more agile!"
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges--;

            if (EffectType is eEffect.StrengthConBuff or eEffect.DexQuickBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
            }
            else if (SpellHandler.Spell.SpellType is eSpellType.BaseArmorFactorBuff or eSpellType.SpecArmorFactorBuff or eSpellType.PaladinArmorFactorBuff)
                ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, true);
            else if (SpellHandler.Spell.SpellType is eSpellType.AllMagicResistBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
            }
            else
            {
                if (EffectType is eEffect.EnduranceRegenBuff)
                    Effectiveness = 1;

                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    if (EffectType is eEffect.MovementSpeedBuff)
                    {
                        Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, EffectType);
                        Owner.OnMaxSpeedChange();
                    }

                    else
                        ApplyBonus(Owner, eBuffBonusCategory.BaseBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
                }
            }

            // "Your agility returns to normal."
            // "{0} loses their graceful edge.""
            OnEffectExpiresMsg(Owner, true, false, true);
            IsBuffActive = false;
        }

        protected static void ApplyBonus(GameLiving owner, eBuffBonusCategory BonusCat, eProperty Property, double Value, double Effectiveness, bool IsSubstracted)
        {
            int effectiveValue = (int)(Value * Effectiveness);

            IPropertyIndexer tblBonusCat;
            if (Property != eProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);
                //Console.WriteLine($"Applying bonus for property {Property} at value {Value} for owner {owner.Name} at effectiveness {Effectiveness} for {effectiveValue} change");
                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                {
                    if(Property == eProperty.ArmorFactor && tblBonusCat[(int)Property] - effectiveValue < 0)
                        tblBonusCat[(int)Property] = 0;
                    else
                        tblBonusCat[(int)Property] -= effectiveValue;

                    if (Property == eProperty.EnduranceRegenerationAmount && tblBonusCat[(int)Property] <= 0)
                        tblBonusCat[(int)Property] = 0;
                }
                    
                else
                    tblBonusCat[(int)Property] += effectiveValue;
                //Console.WriteLine($"Value after: {tblBonusCat[(int)Property]}");
            }
        }

        private static IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryid)
        {
            IPropertyIndexer bonuscat = null;
            switch (categoryid)
            {
                case eBuffBonusCategory.BaseBuff:
                    bonuscat = target.BaseBuffBonusCategory;
                    break;
                case eBuffBonusCategory.SpecBuff:
                    bonuscat = target.SpecBuffBonusCategory;
                    break;
                case eBuffBonusCategory.Debuff:
                    bonuscat = target.DebuffCategory;
                    break;
                case eBuffBonusCategory.Other:
                    bonuscat = target.BuffBonusCategory4;
                    break;
                case eBuffBonusCategory.SpecDebuff:
                    bonuscat = target.SpecDebuffCategory;
                    break;
                case eBuffBonusCategory.AbilityBuff:
                    bonuscat = target.AbilityBonus;
                    break;
                default:
                    //if (log.IsErrorEnabled)
                    //Console.WriteLine("BonusCategory not found " + categoryid + "!");
                    break;
            }
            return bonuscat;
        }
    }
}
