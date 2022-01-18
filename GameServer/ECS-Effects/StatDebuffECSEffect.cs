using System;
using DOL.GS.Effects;
using DOL.GS.Spells;
using DOL.GS.PacketHandler;
using DOL.AI.Brain;
using DOL.GS.PropertyCalc;
using System.Collections.Generic;
using DOL.GS.API;

namespace DOL.GS
{
    public class StatDebuffECSEffect : ECSGameSpellEffect
    {
        public StatDebuffECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }

        public override void OnStartEffect()
        {
            if (Owner is GamePlayer player)
                TryDebuffInterrupt(SpellHandler.Spell, player, SpellHandler.Caster);

            //if our debuff is already on the target, do not reapply effect
            if (Owner.effectListComponent.Effects.ContainsKey(EffectType))
            {
                List<ECSGameSpellEffect> effects = Owner.effectListComponent.GetSpellEffects(EffectType);
                foreach (var e in effects)
                {
                    if (e.SpellHandler.Spell.ID == SpellHandler.Spell.ID && IsBuffActive)
                    {
                        return;
                    }
                }
            }

            if (EffectType == eEffect.StrConDebuff || EffectType == eEffect.DexQuiDebuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Debuffing {prop.ToString()}");
                    ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness, true);
                }
            }
            else
            {
                if (EffectType == eEffect.MovementSpeedDebuff)
                {
                    //// Cannot apply if the effect owner has a charging effect
                    //if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
                    //{
                    //    MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                    //    return;
                    //}

                    //Console.WriteLine("Debuffing Speed for " + e.Owner.Name);
                    //e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID, 1.0 - e.SpellHandler.Spell.Value * 0.01);
                    Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, EffectType,
                        1.0 - SpellHandler.Spell.Value * 0.01);
                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(Owner);

                    (SpellHandler as SpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1,
                        eChatType.CT_Spell);
                    Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)),
                        eChatType.CT_Spell, Owner);
                }
                else
                {
                    bool interruptSent = false;
                    foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    {
                        //Console.WriteLine($"Debuffing {prop.ToString()}");
                        if (EffectType == eEffect.ArmorFactorDebuff)
                            ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness,
                                false);
                        else
                            ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness,
                                true);
                    }
                }
            }

            //IsBuffActive = true;
        }

        public override void OnStopEffect()
        {
            if (EffectType == eEffect.StrConDebuff || EffectType == eEffect.DexQuiDebuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");
                    ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness, false);
                }
            }
            else
            {
                if (EffectType == eEffect.MovementSpeedDebuff)
                {
                    //if (SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                    //{
                    //    new ECSImmunityEffect(Owner, SpellHandler, 60000, (int)PulseFreq, Effectiveness, Icon);
                    //}

                    //e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID);
                    Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, EffectType);
                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(Owner);
                }
                else
                {
                    foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    {
                        //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");

                        if (EffectType == eEffect.ArmorFactorDebuff)
                            ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness,
                                true);
                        else
                            ApplyBonus(Owner, eBuffBonusCategory.Debuff, prop, SpellHandler.Spell.Value, Effectiveness,
                                false);
                    }
                }
            }

            if (EffectType == eEffect.ConstitutionDebuff || EffectType == eEffect.StrConDebuff ||
                EffectType == eEffect.WsConDebuff)
            {
                Owner.StartHealthRegeneration();
            }

            IsBuffActive = false;
        }

        private static void ApplyBonus(GameLiving owner, eBuffBonusCategory BonusCat, eProperty Property, double Value,
            double Effectiveness, bool IsSubstracted)
        {
            int effectiveValue = (int) (Value * Effectiveness);

            IPropertyIndexer tblBonusCat;
            if (Property != eProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);

                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                {
                    tblBonusCat[(int) Property] -= effectiveValue;
                }
                else
                    tblBonusCat[(int) Property] += effectiveValue;
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

        public static void TryDebuffInterrupt(Spell spell, GamePlayer player, GameLiving caster)
        {
            if (spell.ID != 10031 && //BD insta debuffs
                spell.ID != 10032 &&
                spell.ID != 10033 &&
                spell.ID != 9631 && //reaver pbae insta melee damage reductions
                spell.ID != 9632 &&
                spell.ID != 9633 &&
                spell.ID != 9634 &&
                spell.ID != 9635 &&
                spell.ID != 9636 &&
                spell.ID != 9637 &&
                spell.ID != 9601 && //reaver pbae insta abs reductions
                spell.ID != 9602 &&
                spell.ID != 9603 &&
                spell.ID != 9604 &&
                spell.ID != 9605 &&
                spell.ID != 9606)
                return;

            player.StopCurrentSpellcast();
            player.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
        }
    }
}