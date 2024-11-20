using System.Collections.Generic;
using System.Linq;
using DOL.GS.PlayerClass;
using DOL.GS.PropertyCalc;

namespace DOL.GS
{
    public class StatDebuffECSEffect : ECSGameSpellEffect
    {
        private bool _isInSpecDebuffCategory;

        public StatDebuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

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

            eBuffBonusCategory debuffCategory;

            // Put Champion's stat debuffs in the spec debuff category (see `StatCalculator`).
            if (SpellHandler.Caster is GamePlayer playerCaster &&
                playerCaster.CharacterClass is ClassChampion &&
                SpellHandler.SpellLine.KeyName is GlobalSpellsLines.Valor &&
                (EffectService.GetPlayerUpdateFromEffect(EffectType) & EffectService.PlayerUpdate.STATS) != 0)
            {
                debuffCategory = eBuffBonusCategory.SpecDebuff;
                _isInSpecDebuffCategory = true;
            }
            else
                debuffCategory = eBuffBonusCategory.Debuff;

            if (EffectType is eEffect.StrConDebuff or eEffect.DexQuiDebuff)
            {
                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, debuffCategory, property, SpellHandler.Spell.Value, Effectiveness, true);
            }
            else
            {
                if (EffectType is eEffect.MovementSpeedDebuff)
                {
                    //// Cannot apply if the effect owner has a charging effect
                    //if (effect.Owner.EffectList.GetOfType<ChargeEffect>() != null || effect.Owner.TempProperties.GetProperty<bool>("Charging"))
                    //{
                    //    MessageToCaster(effect.Owner.Name + " is moving too fast for this spell to have any effect!", eChatType.CT_SpellResisted);
                    //    return;
                    //}

                    var speedDebuffs = Owner.effectListComponent.GetSpellEffects(eEffect.MovementSpeedDebuff).Where(x => x.SpellHandler.Spell.ID != SpellHandler.Spell.ID);

                    if (speedDebuffs.Any(x => x.SpellHandler.Spell.Value > SpellHandler.Spell.Value))
                        return;

                    foreach (ECSGameSpellEffect effect in speedDebuffs)
                        EffectService.RequestDisableEffect(effect);

                    double effectiveValue = SpellHandler.Spell.Value * Effectiveness;
                    Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, EffectType, 1.0 - effectiveValue * 0.01);
                    Owner.OnMaxSpeedChange();
                }
                else
                {
                    foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    {
                        if (EffectType is eEffect.ArmorFactorDebuff)
                            ApplyBonus(Owner, debuffCategory, property, SpellHandler.Spell.Value, Effectiveness, EffectType is not eEffect.ArmorFactorDebuff);
                    }
                }
            }

            // "Your agility is suppressed!"
            // "{0} seems uncoordinated!"
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            eBuffBonusCategory debuffCategory = _isInSpecDebuffCategory ? eBuffBonusCategory.SpecDebuff : eBuffBonusCategory.Debuff;

            if (EffectType is eEffect.StrConDebuff or eEffect.DexQuiDebuff)
            {
                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, debuffCategory, property, SpellHandler.Spell.Value, Effectiveness, false);
            }
            else
            {
                if (EffectType is eEffect.MovementSpeedDebuff)
                {
                    ECSGameSpellEffect speedDebuff = Owner.effectListComponent.GetBestDisabledSpellEffect(eEffect.MovementSpeedDebuff);

                    if (speedDebuff != null)
                        EffectService.RequestEnableEffect(speedDebuff);

                    Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, EffectType);
                    Owner.OnMaxSpeedChange();
                }
                else
                {
                    foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                        ApplyBonus(Owner, debuffCategory, property, SpellHandler.Spell.Value, Effectiveness, EffectType is eEffect.ArmorFactorDebuff);
                }
            }

            if (EffectType is eEffect.ConstitutionDebuff or eEffect.StrConDebuff or eEffect.WsConDebuff)
                Owner.StartHealthRegeneration();

            // "Your coordination returns."
            // "{0}'s coordination returns."
            OnEffectExpiresMsg(Owner, true, false, true);
            IsBuffActive = false;
        }

        private static void ApplyBonus(GameLiving owner, eBuffBonusCategory BonusCat, eProperty Property, double Value, double Effectiveness, bool IsSubtracted)
        {
            int effectiveValue = (int) Value;

            if (Property is not eProperty.FatigueConsumption)
                effectiveValue = (int) (Value * Effectiveness);

            IPropertyIndexer bonusCategory;

            if (Property is not eProperty.Undefined)
            {
                bonusCategory = GetBonusCategory(owner, BonusCat);

                // This should probably be the opposite?
                // Most values returned by 'DebuffCategory' are modified with 'Math.Abs' because of this.
                if (IsSubtracted)
                    bonusCategory[(int) Property] -= effectiveValue;
                else
                    bonusCategory[(int) Property] += effectiveValue;
            }
        }

        private static IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryId)
        {
            IPropertyIndexer bonusCategory = null;

            switch (categoryId)
            {
                case eBuffBonusCategory.BaseBuff:
                {
                    bonusCategory = target.BaseBuffBonusCategory;
                    break;
                }
                case eBuffBonusCategory.SpecBuff:
                {
                    bonusCategory = target.SpecBuffBonusCategory;
                    break;
                }
                case eBuffBonusCategory.Debuff:
                {
                    bonusCategory = target.DebuffCategory;
                    break;
                }
                case eBuffBonusCategory.Other:
                {
                    bonusCategory = target.BuffBonusCategory4;
                    break;
                }
                case eBuffBonusCategory.SpecDebuff:
                {
                    bonusCategory = target.SpecDebuffCategory;
                    break;
                }
                case eBuffBonusCategory.AbilityBuff:
                {
                    bonusCategory = target.AbilityBonus;
                    break;
                }
                default:
                    break;
            }

            return bonusCategory;
        }

        public static void TryDebuffInterrupt(Spell spell, GamePlayer player, GameLiving caster)
        {
            if (spell.ID is not 10031 and //BD insta debuffs
                not 10032 and
                not 10033 and
                not 9631 and //reaver pbae insta melee damage reductions
                not 9632 and
                not 9633 and
                not 9634 and
                not 9635 and
                not 9636 and
                not 9637 and
                not 9601 and //reaver pbae insta abs reductions
                not 9602 and
                not 9603 and
                not 9604 and
                not 9605 and
                not 9606)
                return;

            if (player != null)
            {
                player.StopCurrentSpellcast();
                player.StartInterruptTimer(player.SpellInterruptDuration, AttackData.eAttackType.Spell, caster);
            }
        }
    }
}
