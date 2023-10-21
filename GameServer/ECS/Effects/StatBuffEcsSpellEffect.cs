using Core.GS.Calculators;
using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.ECS;

public class StatBuffEcsSpellEffect : EcsGameSpellEffect
{
    public StatBuffEcsSpellEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
    }

    public override void OnStartEffect()
    {
        if (this.OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(this.SpellHandler.Spell.ID))
            OwnerPlayer.ActiveBuffCharges++;

        if (EffectType == EEffect.StrengthConBuff || EffectType == EEffect.DexQuickBuff)
        {
            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                //Console.WriteLine($"Buffing {prop.ToString()}");
                ApplyBonus(Owner, EBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
            }
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.ArmorFactorBuff)
        {
            ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, EProperty.ArmorFactor,
                SpellHandler.Spell.Value, Effectiveness, false);
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.PaladinArmorFactorBuff)
        {
            ApplyBonus(Owner, (SpellHandler as PaladinArmorFactorBuff).BonusCategory1, EProperty.ArmorFactor,
                SpellHandler.Spell.Value, Effectiveness, false);
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.AllMagicResistBuff)
        {
            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                ApplyBonus(Owner, EBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
            }
        }
        else
        {
            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                //Console.WriteLine($"Buffing {prop.ToString()}");
                if (EffectType == EEffect.EnduranceRegenBuff)
                    Effectiveness = 1;

                if (EffectType == EEffect.MovementSpeedBuff)
                {
                    if ( /*!Owner.InCombat && */!Owner.IsStealthed)
                    {
                        //Console.WriteLine($"Value before: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                        //e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler, e.SpellHandler.Spell.Value / 100.0);
                        Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, EffectType,
                            SpellHandler.Spell.Value / 100.0);
                        //Console.WriteLine($"Value after: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                        (SpellHandler as SpeedEnhancementSpell).SendUpdates(Owner);
                    }

                    if (Owner.IsStealthed)
                    {
                        EffectService.RequestDisableEffect(this);
                    }
                }

                else
                    ApplyBonus(Owner, EBuffBonusCategory.BaseBuff, prop, SpellHandler.Spell.Value, Effectiveness,
                        false);
            }
        }

        // "You feel more dexterous!"
        // "{0} looks more agile!"
        OnEffectStartsMsg(Owner, true, true, true);


        //IsBuffActive = true;
    }

    public override void OnStopEffect()
    {
        if (this.OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(this.SpellHandler.Spell.ID))
            OwnerPlayer.ActiveBuffCharges--;

        if (EffectType == EEffect.StrengthConBuff || EffectType == EEffect.DexQuickBuff)
        {
            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                //Console.WriteLine($"Canceling {prop.ToString()}");
                ApplyBonus(Owner, EBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
            }
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.ArmorFactorBuff)
        {
            ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, EProperty.ArmorFactor,
                SpellHandler.Spell.Value, Effectiveness, true);
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.PaladinArmorFactorBuff)
        {
            ApplyBonus(Owner, (SpellHandler as PaladinArmorFactorBuff).BonusCategory1, EProperty.ArmorFactor,
                SpellHandler.Spell.Value, Effectiveness, true);
        }
        else if (SpellHandler.Spell.SpellType == ESpellType.AllMagicResistBuff)
        {
            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                ApplyBonus(Owner, EBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
            }
        }
        else
        {
            if (EffectType == EEffect.EnduranceRegenBuff)
                Effectiveness = 1;

            foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
            {
                //Console.WriteLine($"Canceling {prop.ToString()}");


                if (EffectType == EEffect.MovementSpeedBuff)
                {
                    //if (Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed) == SpellHandler.Spell.Value / 100 || Owner.InCombat)
                    //{
                    //Console.WriteLine($"Value before: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                    //e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler);
                    Owner.BuffBonusMultCategory1.Remove((int)EProperty.MaxSpeed, EffectType);
                    //Console.WriteLine($"Value after: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                    (SpellHandler as SpeedEnhancementSpell).SendUpdates(Owner);
                    //}
                }

                else
                    ApplyBonus(Owner, EBuffBonusCategory.BaseBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);

            }
        }

        // "Your agility returns to normal."
        // "{0} loses their graceful edge.""
        OnEffectExpiresMsg(Owner, true, false, true);


        IsBuffActive = false;
    }

    protected static void ApplyBonus(GameLiving owner, EBuffBonusCategory BonusCat, EProperty Property, double Value,
        double Effectiveness, bool IsSubstracted)
    {
        int effectiveValue = (int)(Value * Effectiveness);

        IPropertyIndexer tblBonusCat;
        if (Property != EProperty.Undefined)
        {
            tblBonusCat = GetBonusCategory(owner, BonusCat);
            //Console.WriteLine($"Applying bonus for property {Property} at value {Value} for owner {owner.Name} at effectiveness {Effectiveness} for {effectiveValue} change");
            //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
            if (IsSubstracted)
            {
                if (Property == EProperty.ArmorFactor && tblBonusCat[(int)Property] - effectiveValue < 0)
                    tblBonusCat[(int)Property] = 0;
                else
                    tblBonusCat[(int)Property] -= effectiveValue;

                if (Property == EProperty.EnduranceRegenerationRate && tblBonusCat[(int)Property] <= 0)
                    tblBonusCat[(int)Property] = 0;
            }

            else
                tblBonusCat[(int)Property] += effectiveValue;
            //Console.WriteLine($"Value after: {tblBonusCat[(int)Property]}");
        }
    }

    private static IPropertyIndexer GetBonusCategory(GameLiving target, EBuffBonusCategory categoryid)
    {
        IPropertyIndexer bonuscat = null;
        switch (categoryid)
        {
            case EBuffBonusCategory.BaseBuff:
                bonuscat = target.BaseBuffBonusCategory;
                break;
            case EBuffBonusCategory.SpecBuff:
                bonuscat = target.SpecBuffBonusCategory;
                break;
            case EBuffBonusCategory.Debuff:
                bonuscat = target.DebuffCategory;
                break;
            case EBuffBonusCategory.Other:
                bonuscat = target.BuffBonusCategory4;
                break;
            case EBuffBonusCategory.SpecDebuff:
                bonuscat = target.SpecDebuffCategory;
                break;
            case EBuffBonusCategory.AbilityBuff:
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