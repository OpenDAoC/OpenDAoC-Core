using System;
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
        private const string SERVICE_NAME = nameof(EffectListService);
        private static List<EffectListComponent> _list;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<EffectListComponent>(EntityManager.EntityType.EffectListComponent, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            EffectListComponent effectListComponent = _list[index];

            try
            {
                if (effectListComponent?.EntityManagerId.IsSet != true)
                    return;

                long startTick = GameLoop.GetCurrentTime();
                HandleEffects(effectListComponent);
                effectListComponent.SendPlayerUpdates();
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > 25)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, effectListComponent, effectListComponent.Owner);
            }
        }

        private static void HandleEffects(EffectListComponent effectListComponent)
        {
            if (effectListComponent.Effects.Count == 0)
            {
                EntityManager.Remove(effectListComponent);
                return;
            }

            // Pause if the owner is changing region.
            // Also includes respawning NPCs, so this relies on the reaper service ticking last, otherwise effects wouldn't be cancelled.
            if (effectListComponent.Owner.ObjectState is GameObject.eObjectState.Inactive)
                return;

            List<ECSGameEffect> effectsList = [];

            lock (effectListComponent.EffectsLock)
            {
                foreach (var pair in effectListComponent.Effects)
                    effectsList.AddRange(pair.Value);
            }

            foreach (ECSGameEffect e in effectsList)
            {
                if (!e.Owner.IsAlive)
                {
                    EffectService.RequestCancelEffect(e);
                    continue;
                }

                if (e is ECSGameAbilityEffect)
                {
                    if (e.NextTick != 0 && ServiceUtils.ShouldTickAdjust(ref e.NextTick))
                    {
                        e.OnEffectPulse();
                        e.NextTick += e.PulseFreq;
                    }

                    if (e.Duration > 0 && ServiceUtils.ShouldTick(e.ExpireTick))
                        EffectService.RequestCancelEffect(e);

                    continue;
                }

                if (e is ECSGameSpellEffect effect)
                {
                    ISpellHandler spellHandler = effect.SpellHandler;
                    Spell spell = spellHandler.Spell;
                    GameLiving caster = spellHandler.Caster;

                    if (!effect.IsConcentrationEffect() || spell.IsFocus)
                    {
                        if (effect is ECSPulseEffect pulseEffect)
                        {
                            if (!caster.ActivePulseSpells.ContainsKey(spell.SpellType))
                                EffectService.RequestCancelEffect(pulseEffect);
                            else
                            {
                                if (ServiceUtils.ShouldTickAdjust(ref pulseEffect.ExpireTick))
                                {
                                    if (spell.PulsePower > 0)
                                    {
                                        if (caster.Mana >= spell.PulsePower)
                                        {
                                            caster.Mana -= spell.PulsePower;
                                            spellHandler.StartSpell(null);
                                            pulseEffect.ExpireTick += pulseEffect.PulseFreq;
                                        }
                                        else
                                        {
                                            ((SpellHandler) spellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                            EffectService.RequestCancelConcEffect(pulseEffect);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        spellHandler.StartSpell(null);
                                        pulseEffect.ExpireTick += pulseEffect.PulseFreq;
                                    }

                                    if (spell.IsHarmful && spell.SpellType != eSpellType.SpeedDecrease)
                                    {
                                        if (!pulseEffect.Owner.IsMezzed && !pulseEffect.Owner.IsStunned)
                                            ((SpellHandler) spellHandler).SendCastAnimation();
                                    }
                                }

                                List<GameLiving> livings = null;

                                foreach (var pair in pulseEffect.ChildEffects)
                                {
                                    ECSGameSpellEffect childEffect = pair.Value;

                                    if (ServiceUtils.ShouldTickNoEarly(childEffect.ExpireTick))
                                    {
                                        livings ??= new();
                                        livings.Add(pair.Key);
                                        EffectService.RequestCancelEffect(childEffect);
                                    }
                                }

                                if (livings != null)
                                {
                                    foreach (GameLiving living in livings)
                                    {
                                        pulseEffect.ChildEffects.Remove(living);
                                    }
                                }
                            }
                        }
                        else if (ServiceUtils.ShouldTick(effect.ExpireTick))
                        {
                            // A pulse effect cancels its own child effects to prevent them from being cancelled and immediately reapplied.
                            // So only cancel them if their source is no longer active.
                            if (!spell.IsPulsing || spellHandler.PulseEffect?.IsBuffActive != true)
                                EffectService.RequestCancelEffect(effect);
                        }
                    }

                    if (effect is not ECSImmunityEffect && effect.EffectType != eEffect.Pulse && effect.SpellHandler.Spell.SpellType == eSpellType.SpeedDecrease)
                    {
                        if (ServiceUtils.ShouldTick(e.ExpireTick))
                        {
                            double factor = 2.0 - (effect.Duration - effect.GetRemainingTimeForClient()) / (double)(effect.Duration >> 1);

                            if (factor < 0)
                                factor = 0;
                            else if (factor > 1)
                                factor = 1;

                            effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.EffectType, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);
                            effect.Owner.OnMaxSpeedChange();
                            effect.NextTick += effect.TickInterval;

                            if (factor <= 0)
                                effect.ExpireTick = 0;
                        }
                    }

                    if (effect.NextTick != 0 && ServiceUtils.ShouldTickAdjust(ref effect.NextTick) && !ServiceUtils.ShouldTick(effect.ExpireTick))
                    {
                        effect.OnEffectPulse();
                        effect.NextTick += effect.PulseFreq;
                    }

                    if (effect.IsConcentrationEffect() && ServiceUtils.ShouldTickAdjust(ref effect.NextTick))
                    {
                        int radiusToCheck = effect.SpellHandler.Spell.SpellType != eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500;
                        bool isWithinRadius = effect.SpellHandler.Caster.IsWithinRadius(effect.Owner, radiusToCheck);

                        // Check if player is too far away from Caster for Concentration buff, or back in range.
                        if (!isWithinRadius)
                        {
                            if (!effect.IsDisabled)
                            {
                                ECSGameSpellEffect disabled = null;
                                if (effect.Owner.effectListComponent.GetSpellEffects(effect.EffectType).Count > 1)
                                    disabled = effect.Owner.effectListComponent.GetBestDisabledSpellEffect(effect.EffectType);

                                EffectService.RequestDisableEffect(effect);

                                if (disabled != null)
                                    EffectService.RequestEnableEffect(disabled);
                            }
                        }
                        else if (effect.IsDisabled)
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

                        effect.NextTick += effect.PulseFreq;
                    }
                }
            }
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null && spellType == eSpellType.None)
                    return effects.FirstOrDefault();
                else if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e.SpellHandler.Spell.SpellType == spellType).FirstOrDefault();
                else
                    return null;
            }
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            if (target == null)
                return null;

            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<ECSGameEffect> effects);

                if (effects != null)
                    return effects.OfType<ECSGameSpellEffect>().Where(e => e != null && (spellType == eSpellType.None || e.SpellHandler.Spell.SpellType == spellType)).FirstOrDefault();
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
