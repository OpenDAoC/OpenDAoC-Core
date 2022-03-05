using System;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.PropertyCalc;

namespace DOL.GS
{
    public class StatBuffECSEffect : ECSGameSpellEffect
    {        
        public StatBuffECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            
            if (EffectType == eEffect.StrengthConBuff || EffectType == eEffect.DexQuickBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Buffing {prop.ToString()}");
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
                }
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.ArmorFactorBuff)
            {
                ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, false);
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.PaladinArmorFactorBuff)
            {
                ApplyBonus(Owner, (SpellHandler as PaladinArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, false);
            }
            else
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Buffing {prop.ToString()}");
                    if (EffectType == eEffect.EnduranceRegenBuff)
                        Effectiveness = 1;

                    if (EffectType == eEffect.MovementSpeedBuff)
                    {
                        if (/*!Owner.InCombat && */!Owner.IsStealthed)
                        {
                            //Console.WriteLine($"Value before: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                            //e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler, e.SpellHandler.Spell.Value / 100.0);
                            Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, EffectType, SpellHandler.Spell.Value / 100.0);
                            //Console.WriteLine($"Value after: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                            (SpellHandler as SpeedEnhancementSpellHandler).SendUpdates(Owner);
                        }
                        if (Owner.IsStealthed)
                        {
                            EffectService.RequestDisableEffect(this);
                        }
                    }
                    
                    else
                        ApplyBonus(Owner, eBuffBonusCategory.BaseBuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
                }
            }
            
            // "You feel more dexterous!"
            // "{0} looks more agile!"
            OnEffectStartsMsg(Owner, true, false, true);

            
            //IsBuffActive = true;
        }

        public override void OnStopEffect()
        {
            if (EffectType == eEffect.StrengthConBuff || EffectType == eEffect.DexQuickBuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Canceling {prop.ToString()}");
                    ApplyBonus(Owner, eBuffBonusCategory.SpecBuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
                }
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.ArmorFactorBuff)
            {
                ApplyBonus(Owner, (SpellHandler as ArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, true);
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.PaladinArmorFactorBuff)
            {
                ApplyBonus(Owner, (SpellHandler as PaladinArmorFactorBuff).BonusCategory1, eProperty.ArmorFactor, SpellHandler.Spell.Value, Effectiveness, true);
            }
            else
            {
                if (EffectType == eEffect.EnduranceRegenBuff)
                    Effectiveness = 1;

                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Canceling {prop.ToString()}");


                    if (EffectType == eEffect.MovementSpeedBuff)
                    {
                        //if (Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed) == SpellHandler.Spell.Value / 100 || Owner.InCombat)
                        //{
                            //Console.WriteLine($"Value before: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                            //e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler);
                            Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, EffectType);
                            //Console.WriteLine($"Value after: {Owner.BuffBonusMultCategory1.Get((int)eProperty.MaxSpeed)}");
                            (SpellHandler as SpeedEnhancementSpellHandler).SendUpdates(Owner);
                        //}
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
                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                {
                    if(Property == eProperty.ArmorFactor && tblBonusCat[(int)Property] - effectiveValue < 0)
                        tblBonusCat[(int)Property] = 0;
                    else
                        tblBonusCat[(int)Property] -= effectiveValue;

                    if (Property == eProperty.EnduranceRegenerationRate && tblBonusCat[(int)Property] <= 0)
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