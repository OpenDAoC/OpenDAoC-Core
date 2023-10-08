using System.Collections.Generic;
using System.Linq;
using DOL.GS.PlayerClass;
using DOL.GS.PropertyCalc;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class StatDebuffEcsSpellEffect : EcsGameSpellEffect
    {
        public StatDebuffEcsSpellEffect(EcsGameEffectInitParams initParams)
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
                List<EcsGameSpellEffect> effects = Owner.effectListComponent.GetSpellEffects(EffectType);
                foreach (var e in effects)
                {
                    if (e.SpellHandler.Spell.ID == SpellHandler.Spell.ID && IsBuffActive)
                    {
                        return;
                    }
                }
            }

            EBuffBonusCategory debuffCategory = (Caster as GamePlayer)?.CharacterClass is ClassChampion ? EBuffBonusCategory.SpecDebuff : EBuffBonusCategory.Debuff;

            if (EffectType == EEffect.StrConDebuff || EffectType == EEffect.DexQuiDebuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Debuffing {prop.ToString()}");
                    ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness, true);
                }
            }
            else
            {
                if (EffectType == EEffect.MovementSpeedDebuff)
                {
                    //// Cannot apply if the effect owner has a charging effect
                    //if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.getProperty("Charging", false))
                    //{
                    //    MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                    //    return;
                    //}

                    //Console.WriteLine("Debuffing Speed for " + e.Owner.Name);
                    //e.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID, 1.0 - e.SpellHandler.Spell.Value * 0.01);

                    var speedDebuffs = Owner.effectListComponent.GetSpellEffects(EEffect.MovementSpeedDebuff)
                                                                .Where(x => x.SpellHandler.Spell.ID != this.SpellHandler.Spell.ID);

                    if (speedDebuffs.Any(x => x.SpellHandler.Spell.Value > this.SpellHandler.Spell.Value))
                    {
                        return;
                    }

                    foreach (var effect in speedDebuffs)
                    {
                        EffectService.RequestDisableEffect(effect);
                    }

                    var effectiveValue = SpellHandler.Spell.Value * Effectiveness;

                    Owner.BuffBonusMultCategory1.Set((int) EProperty.MaxSpeed, EffectType,
                        1.0 - effectiveValue * 0.01);
                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(Owner);

                }
                else
                {
                    bool interruptSent = false;

                    foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    {
                        //Console.WriteLine($"Debuffing {prop.ToString()}");
                        if (EffectType == EEffect.ArmorFactorDebuff)
                            ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness,
                                false);
                        else
                            ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness,
                                true);
                    }
                }
            }

            // "Your agility is suppressed!"
            // "{0} seems uncoordinated!"
            OnEffectStartsMsg(Owner, true, true, true);
            
            //IsBuffActive = true;
        }

        public override void OnStopEffect()
        {
            EBuffBonusCategory debuffCategory = (Caster as GamePlayer)?.CharacterClass is ClassChampion ? EBuffBonusCategory.SpecDebuff : EBuffBonusCategory.Debuff;

            if (EffectType == EEffect.StrConDebuff || EffectType == EEffect.DexQuiDebuff)
            {
                foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                {
                    //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");
                    ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness, false);
                }
            }
            else
            {
                if (EffectType == EEffect.MovementSpeedDebuff)
                {
                    //if (SpellHandler.Spell.SpellType == eSpellType.SpeedDecrease)
                    //{
                    //    new ECSImmunityEffect(Owner, SpellHandler, 60000, (int)PulseFreq, Effectiveness, Icon);
                    //}

                    //e.Owner.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, e.SpellHandler.Spell.ID);

                    var speedDebuff = Owner.effectListComponent.GetBestDisabledSpellEffect(EEffect.MovementSpeedDebuff);

                    if (speedDebuff != null)
                    {
                        EffectService.RequestEnableEffect(speedDebuff);
                    }

                    Owner.BuffBonusMultCategory1.Remove((int) EProperty.MaxSpeed, EffectType);
                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(Owner);
                }
                else
                {
                    foreach (var prop in EffectService.GetPropertiesFromEffect(EffectType))
                    {
                        //Console.WriteLine($"Canceling {prop.ToString()} on {e.Owner}.");

                        if (EffectType == EEffect.ArmorFactorDebuff)
                            ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness,
                                true);
                        else
                            ApplyBonus(Owner, debuffCategory, prop, SpellHandler.Spell.Value, Effectiveness,
                                false);
                    }
                }
            }

            if (EffectType == EEffect.ConstitutionDebuff || EffectType == EEffect.StrConDebuff ||
                EffectType == EEffect.WsConDebuff)
            {
                Owner.StartHealthRegeneration();
            }
            
            // "Your coordination returns."
            // "{0}'s coordination returns."
            OnEffectExpiresMsg(Owner, true, false, true);

            IsBuffActive = false;
        }

        private static void ApplyBonus(GameLiving owner, EBuffBonusCategory BonusCat, EProperty Property, double Value,
            double Effectiveness, bool IsSubstracted)
        {
            
            int effectiveValue = (int) Value;

            if (Property != EProperty.FatigueConsumption)
                effectiveValue = (int)(Value * Effectiveness);

            IPropertyIndexer tblBonusCat;
            if (Property != EProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);

                // This should probably be the opposite?
                // Most values returned by 'DebuffCategory' are modified with 'Math.Abs' because of this.
                if (IsSubstracted)
                    tblBonusCat[(int) Property] -= effectiveValue;
                else
                    tblBonusCat[(int) Property] += effectiveValue;
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

            player?.StopCurrentSpellcast();
            player?.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
        }
    }
}
