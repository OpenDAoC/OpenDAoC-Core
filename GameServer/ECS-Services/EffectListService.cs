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

            foreach (ECSGameEffect effect in effectsList)
            {
                if (!effect.Owner.IsAlive)
                    EffectService.RequestCancelEffect(effect);
                else if (effect is ECSGameAbilityEffect abilityEffect)
                    HandleAbilityEffect(abilityEffect);
                else if (effect is ECSGameSpellEffect spellEffect)
                    HandleSpellEffect(spellEffect);
            }

            static void HandleAbilityEffect(ECSGameAbilityEffect abilityEffect)
            {
                if (abilityEffect.NextTick != 0 && ServiceUtils.ShouldTickAdjust(ref abilityEffect.NextTick))
                {
                    abilityEffect.OnEffectPulse();
                    abilityEffect.NextTick += abilityEffect.PulseFreq;
                }

                if (abilityEffect.Duration > 0 && ServiceUtils.ShouldTick(abilityEffect.ExpireTick))
                    EffectService.RequestCancelEffect(abilityEffect);
            }

            static void HandleSpellEffect(ECSGameSpellEffect spellEffect)
            {
                ISpellHandler spellHandler = spellEffect.SpellHandler;
                Spell spell = spellHandler.Spell;
                GameLiving caster = spellHandler.Caster;
                bool isConcentrationEffect = spellEffect.IsConcentrationEffect() && !spell.IsFocus;

                if (isConcentrationEffect && spellEffect.IsAllowedToPulse)
                {
                    HandleConcentrationEffect(spellEffect);
                    return;
                }

                if (ServiceUtils.ShouldTick(spellEffect.ExpireTick))
                {
                    // A pulse effect cancels its own child effects to prevent them from being cancelled and immediately reapplied.
                    // So only cancel them if their source is no longer active.
                    if (!spell.IsPulsing || spellHandler.PulseEffect?.IsBuffActive != true)
                        EffectService.RequestCancelEffect(spellEffect);
                }

                // Make sure the effect actually has a next tick scheduled since some spells are marked as pulsing but actually don't.
                if (spellEffect.IsAllowedToPulse)
                    HandlePulsingEffect(spellEffect, spell, spellHandler, caster);

                static void HandleConcentrationEffect(ECSGameSpellEffect spellEffect)
                {
                    if (!ServiceUtils.ShouldTickAdjust(ref spellEffect.NextTick))
                        return;

                    int radiusToCheck = spellEffect.SpellHandler.Spell.SpellType is not eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500;
                    bool isWithinRadius = spellEffect.SpellHandler.Caster.IsWithinRadius(spellEffect.Owner, radiusToCheck);

                    // Check if player is too far away from Caster for Concentration buff, or back in range.
                    if (!isWithinRadius)
                    {
                        if (!spellEffect.IsDisabled)
                        {
                            ECSGameSpellEffect disabled = null;
                            if (spellEffect.Owner.effectListComponent.GetSpellEffects(spellEffect.EffectType).Count > 1)
                                disabled = spellEffect.Owner.effectListComponent.GetBestDisabledSpellEffect(spellEffect.EffectType);

                            EffectService.RequestDisableEffect(spellEffect);

                            if (disabled != null)
                                EffectService.RequestEnableEffect(disabled);
                        }
                    }
                    else if (spellEffect.IsDisabled)
                    {
                        //Check if this effect is better than currently enabled effects. Enable this effect and disable other effect if true.
                        ECSGameSpellEffect enabled = null;
                        spellEffect.Owner.effectListComponent.Effects.TryGetValue(spellEffect.EffectType, out List<ECSGameEffect> sameEffectTypeEffects);
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
                                        isBest = spellEffect.SpellHandler.Spell.Value > eff.SpellHandler.Spell.Value;
                                    }
                                }
                            }
                        }

                        if (isBest)
                        {
                            EffectService.RequestEnableEffect(spellEffect);

                            if (enabled != null)
                                EffectService.RequestDisableEffect(enabled);
                        }
                    }

                    spellEffect.NextTick += spellEffect.PulseFreq;
                }

                static void HandlePulsingEffect(ECSGameSpellEffect spellEffect, Spell spell, ISpellHandler spellHandler, GameLiving caster)
                {
                    if (!ServiceUtils.ShouldTickAdjust(ref spellEffect.NextTick))
                        return;

                    // Not every pulsing effect is a `ECSPulseEffect`. Snares and roots decreasing effect are also handled as pulsing spells for example.
                    if (spellEffect is ECSPulseEffect pulseEffect)
                    {
                        if (!caster.ActivePulseSpells.ContainsKey(spell.SpellType))
                            EffectService.RequestCancelEffect(pulseEffect);
                        else
                        {
                            if (spell.PulsePower > 0)
                            {
                                if (caster.Mana >= spell.PulsePower)
                                {
                                    caster.Mana -= spell.PulsePower;
                                    spellHandler.StartSpell(null);
                                }
                                else
                                {
                                    (spellHandler as SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                    EffectService.RequestCancelConcEffect(pulseEffect);
                                    return;
                                }
                            }
                            else
                                spellHandler.StartSpell(null);

                            if (spell.IsHarmful && spell.SpellType is not eSpellType.SpeedDecrease)
                            {
                                if (!pulseEffect.Owner.IsMezzed && !pulseEffect.Owner.IsStunned)
                                    (spellHandler as SpellHandler).SendCastAnimation();
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
                    else if (spellEffect is not ECSImmunityEffect && spellEffect.EffectType is not eEffect.Pulse && spellEffect.SpellHandler.Spell.SpellType is eSpellType.SpeedDecrease)
                    {
                        double factor = 2.0 - (spellEffect.Duration - spellEffect.GetRemainingTimeForClient()) / (spellEffect.Duration * 0.5);

                        if (factor < 0)
                            factor = 0;
                        else if (factor > 1)
                            factor = 1;

                        spellEffect.Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, spellEffect.EffectType, 1.0 - spellEffect.SpellHandler.Spell.Value * factor * 0.01);
                        spellEffect.Owner.OnMaxSpeedChange();

                        if (factor <= 0)
                        {
                            EffectService.RequestImmediateCancelEffect(spellEffect);
                            return;
                        }
                    }

                    spellEffect.OnEffectPulse();
                    spellEffect.NextTick += spellEffect.PulseFreq;
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
