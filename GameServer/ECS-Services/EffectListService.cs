using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class EffectListService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "EffectListService";

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<EffectListComponent> list = EntityManager.GetAll<EffectListComponent>(EntityManager.EntityType.EffectListComponent);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.EffectListComponent) + 1, i =>
            {
                EffectListComponent e = list[i];

                if (e == null)
                    return;

                long startTick = GameTimer.GetTickCount();
                HandleEffects(e, tick);
                long stopTick = GameTimer.GetTickCount();

                if ((stopTick - startTick) > 25)
                    log.Warn($"Long EffectListService.Tick for {e.Owner.Name}({e.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void HandleEffects(EffectListComponent effectListComponent, long tick)
        {
            if (!effectListComponent.Effects.Any())
            {
                effectListComponent.EntityManagerId = EntityManager.Remove(EntityManager.EntityType.EffectListComponent, effectListComponent.EntityManagerId);
                return;
            }

            List<ECSGameEffect> effects = new(10);

            lock (effectListComponent.EffectsLock)
            {
                List<List<ECSGameEffect>> currentEffects = effectListComponent.Effects.Values.ToList();

                for (int i = 0; i < currentEffects.Count; i++)
                    effects.AddRange(currentEffects[i]);
            }

            for (int j = 0; j < effects.Count; j++)
            {
                ECSGameEffect e = effects[j];

                if (e is null)
                    continue;

                if (!e.Owner.IsAlive || e.Owner.ObjectState == GameObject.eObjectState.Deleted)
                {
                    EffectService.RequestCancelEffect(e);
                    continue;
                }

                // TEMP - A lot of the code below assumes effects come from spells but many effects come from abilities (Sprint, Stealth, RAs, etc)
                // This will need a better refactor later but for now this prevents crashing while working on porting over non-spell based effects to our system.
                if (e is ECSGameAbilityEffect)
                {
                    if (e.NextTick != 0 && tick > e.NextTick)
                        e.OnEffectPulse();

                    if (e.Duration > 0 && tick > e.ExpireTick)
                        EffectService.RequestCancelEffect(e);

                    continue;
                }
                else if (e is ECSGameSpellEffect effect)
                {
                    if (tick > effect.ExpireTick && (!effect.IsConcentrationEffect() || effect.SpellHandler.Spell.IsFocus))
                    {
                        if (effect.EffectType == eEffect.Pulse && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType))
                        {
                            if (effect.SpellHandler.Spell.PulsePower > 0)
                            {
                                if (effect.SpellHandler.Caster.Mana >= effect.SpellHandler.Spell.PulsePower)
                                {
                                    effect.SpellHandler.Caster.Mana -= effect.SpellHandler.Spell.PulsePower;
                                    effect.SpellHandler.StartSpell(null);
                                    effect.ExpireTick += effect.PulseFreq;
                                }
                                else
                                {
                                    ((SpellHandler)effect.SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                    EffectService.RequestCancelConcEffect(effect);
                                    continue;
                                }
                            }
                            else
                            {
                                effect.SpellHandler.StartSpell(null);
                                effect.ExpireTick += effect.PulseFreq;
                            }

                            if (effect.SpellHandler.Spell.IsHarmful && effect.SpellHandler.Spell.SpellType != (byte)eSpellType.Charm && effect.SpellHandler.Spell.SpellType != (byte)eSpellType.SpeedDecrease)
                            {
                                if (!(effect.Owner.IsMezzed || effect.Owner.IsStunned))
                                    ((SpellHandler)effect.SpellHandler).SendCastAnimation();
                            }
                        }
                        else
                        {
                            if (effect.SpellHandler.Spell.IsPulsing && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType) &&
                                effect.ExpireTick >= (effect.LastTick + (effect.Duration > 0 ? effect.Duration : effect.PulseFreq)))
                            {
                                //Add time to effect to make sure the spell refreshes instead of cancels
                                effect.ExpireTick += GameLoop.TickRate;
                                effect.LastTick = tick;
                            }
                            else
                                EffectService.RequestCancelEffect(effect);
                        }
                    }

                    if (effect is not ECSImmunityEffect && effect.EffectType != eEffect.Pulse && effect.SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                    {
                        if (tick > effect.NextTick)
                        {
                            double factor = 2.0 - (effect.Duration - effect.GetRemainingTimeForClient()) / (double)(effect.Duration >> 1);

                            if (factor < 0)
                                factor = 0;
                            else if (factor > 1)
                                factor = 1;

                            //effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.SpellHandler.Spell.ID, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);
                            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.EffectType, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);

                            UnbreakableSpeedDecreaseSpellHandler.SendUpdates(effect.Owner);
                            effect.NextTick = tick + effect.TickInterval;
                            if (factor <= 0)
                                effect.ExpireTick = tick - 1;
                        }
                    }

                    if (effect.NextTick != 0 && tick >= effect.NextTick && tick < effect.ExpireTick)
                        effect.OnEffectPulse();

                    if (effect.IsConcentrationEffect() && tick > effect.NextTick)
                    {
                        //Check if player is too far away from Caster for Concentration buff.
                        if (!effect.SpellHandler.Caster.
                            IsWithinRadius(effect.Owner,
                            effect.SpellHandler.Spell.SpellType != (byte)eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500)
                            && !effect.IsDisabled)
                        {
                            ECSGameSpellEffect disabled = null;
                            if (effect.Owner.effectListComponent.GetSpellEffects(effect.EffectType).Count > 1)
                                disabled = effect.Owner.effectListComponent.GetBestDisabledSpellEffect(effect.EffectType);

                            EffectService.RequestDisableEffect(effect);

                            if (disabled != null)
                                EffectService.RequestEnableEffect(disabled);
                        }
                        //Check if player is back in range of Caster for Concentration buff.
                        else if (effect.SpellHandler.Caster.IsWithinRadius(effect.Owner,
                            effect.SpellHandler.Spell.SpellType != (byte)eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500)
                            && effect.IsDisabled)
                        {
                            //Check if this effect is better than currently enabled effects. Enable this effect and disable other effect if true.
                            ECSGameSpellEffect enabled = null;
                            effect.Owner.effectListComponent.Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> sameEffectTypeEffects);
                            bool isBest = false;

                            if (sameEffectTypeEffects.Count == 1)
                                isBest = true;
                            else if (sameEffectTypeEffects.Count > 1)
                            {
                                foreach (var tmpEff in sameEffectTypeEffects)
                                {
                                    if (tmpEff is ECSGameSpellEffect eff)
                                    {
                                        //Check only against enabled spells
                                        if (!eff.IsDisabled)
                                        {
                                            enabled = eff;
                                            isBest = effect.SpellHandler.Spell.Value > eff.SpellHandler.Spell.Value;
                                        }
                                    }
                                }
                            }

                            if (isBest)
                            {
                                EffectService.RequestEnableEffect(effect);

                                if (enabled != null)
                                    EffectService.RequestDisableEffect(enabled);
                            }
                        }

                        effect.NextTick = tick + effect.PulseFreq;
                    }
                }
            }
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Null)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null && spellType == eSpellType.Null)
                    return effects.FirstOrDefault();
                else if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e.SpellHandler.Spell.SpellType == (byte) spellType).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Null)
        {
            if (target == null)
                return null;

            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e != null && (spellType == eSpellType.Null || e.SpellHandler.Spell.SpellType == (byte) spellType)).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSGameAbilityEffect) effects.Where(e => e is ECSGameAbilityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSImmunityEffect) effects.Where(e => e is ECSImmunityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(eEffect.Pulse, out List<ECSGameEffect> effects);

                if (effects != null)
                    return (ECSPulseEffect) effects.Where(e => e is ECSPulseEffect && e.SpellHandler.Spell == spell).FirstOrDefault();
                else
                    return null;
            }
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            if (target == null || target.effectListComponent == null)
                return false;

            ECSGameEffect effectToCancel;

            lock (target.effectListComponent.EffectsLock)
            {
                if (!target.effectListComponent.ContainsEffectForEffectType(effectType))
                    return false;

                effectToCancel = GetEffectOnTarget(target, effectType);

                if (effectToCancel == null)
                    return false;

                EffectService.RequestImmediateCancelEffect(effectToCancel);
                return true;
            }
        }
    }
}
