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
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<EffectListComponent> list = EntityMgr.UpdateAndGetAll<EffectListComponent>(EntityMgr.EntityType.EffectListComponent, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                EffectListComponent e = list[i];

                if (e == null)
                    return;

                long startTick = GameLoop.GetCurrentTime();
                HandleEffects(e, tick);
                long stopTick = GameLoop.GetCurrentTime();

                if ((stopTick - startTick) > 25)
                    log.Warn($"Long EffectListService.Tick for {e.Owner.Name}({e.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void HandleEffects(EffectListComponent effectListComponent, long tick)
        {
            if (!effectListComponent.Effects.Any())
            {
                EntityMgr.Remove(EntityMgr.EntityType.EffectListComponent, effectListComponent);
                return;
            }

            List<EcsGameEffect> effects = new(10);

            lock (effectListComponent.EffectsLock)
            {
                List<List<EcsGameEffect>> currentEffects = effectListComponent.Effects.Values.ToList();

                for (int i = 0; i < currentEffects.Count; i++)
                    effects.AddRange(currentEffects[i]);
            }

            for (int j = 0; j < effects.Count; j++)
            {
                EcsGameEffect e = effects[j];

                if (e is null)
                    continue;

                if (!e.Owner.IsAlive || e.Owner.ObjectState == GameObject.eObjectState.Deleted)
                {
                    EffectService.RequestCancelEffect(e);
                    continue;
                }

                // TEMP - A lot of the code below assumes effects come from spells but many effects come from abilities (Sprint, Stealth, RAs, etc)
                // This will need a better refactor later but for now this prevents crashing while working on porting over non-spell based effects to our system.
                if (e is EcsGameAbilityEffect)
                {
                    if (e.NextTick != 0 && tick > e.NextTick)
                        e.OnEffectPulse();

                    if (e.Duration > 0 && tick > e.ExpireTick)
                        EffectService.RequestCancelEffect(e);

                    continue;
                }
                else if (e is EcsGameSpellEffect effect)
                {
                    if (tick > effect.ExpireTick && (!effect.IsConcentrationEffect() || effect.SpellHandler.Spell.IsFocus))
                    {
                        if (effect.EffectType == EEffect.Pulse && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType))
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
                                    ((SpellHandler)effect.SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", EChatType.CT_SpellExpires);
                                    EffectService.RequestCancelConcEffect(effect);
                                    continue;
                                }
                            }
                            else
                            {
                                effect.SpellHandler.StartSpell(null);
                                effect.ExpireTick += effect.PulseFreq;
                            }

                            if (effect.SpellHandler.Spell.IsHarmful && effect.SpellHandler.Spell.SpellType != ESpellType.Charm && effect.SpellHandler.Spell.SpellType != ESpellType.SpeedDecrease)
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
                                effect.ExpireTick += GameLoop.TICK_RATE;
                                effect.LastTick = tick;
                            }
                            else
                                EffectService.RequestCancelEffect(effect);
                        }
                    }

                    if (effect is not EcsImmunityEffect && effect.EffectType != EEffect.Pulse && effect.SpellHandler.Spell.SpellType == ESpellType.SpeedDecrease)
                    {
                        if (tick > effect.NextTick)
                        {
                            double factor = 2.0 - (effect.Duration - effect.GetRemainingTimeForClient()) / (double)(effect.Duration >> 1);

                            if (factor < 0)
                                factor = 0;
                            else if (factor > 1)
                                factor = 1;

                            //effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.SpellHandler.Spell.ID, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);
                            effect.Owner.BuffBonusMultCategory1.Set((int)EProperty.MaxSpeed, effect.EffectType, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);

                            UnbreakableSpeedDecreaseHandler.SendUpdates(effect.Owner);
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
                            effect.SpellHandler.Spell.SpellType != ESpellType.EnduranceRegenBuff ? ServerProperties.ServerProperties.BUFF_RANGE > 0 ? ServerProperties.ServerProperties.BUFF_RANGE : 5000 : 1500)
                            && !effect.IsDisabled)
                        {
                            EcsGameSpellEffect disabled = null;
                            if (effect.Owner.effectListComponent.GetSpellEffects(effect.EffectType).Count > 1)
                                disabled = effect.Owner.effectListComponent.GetBestDisabledSpellEffect(effect.EffectType);

                            EffectService.RequestDisableEffect(effect);

                            if (disabled != null)
                                EffectService.RequestEnableEffect(disabled);
                        }
                        //Check if player is back in range of Caster for Concentration buff.
                        else if (effect.SpellHandler.Caster.IsWithinRadius(effect.Owner,
                            effect.SpellHandler.Spell.SpellType != ESpellType.EnduranceRegenBuff ? ServerProperties.ServerProperties.BUFF_RANGE > 0 ? ServerProperties.ServerProperties.BUFF_RANGE : 5000 : 1500)
                            && effect.IsDisabled)
                        {
                            //Check if this effect is better than currently enabled effects. Enable this effect and disable other effect if true.
                            EcsGameSpellEffect enabled = null;
                            effect.Owner.effectListComponent.Effects.TryGetValue(effect.EffectType, out List<EcsGameEffect> sameEffectTypeEffects);
                            bool isBest = false;

                            if (sameEffectTypeEffects.Count == 1)
                                isBest = true;
                            else if (sameEffectTypeEffects.Count > 1)
                            {
                                foreach (var tmpEff in sameEffectTypeEffects)
                                {
                                    if (tmpEff is EcsGameSpellEffect eff)
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

        public static EcsGameEffect GetEffectOnTarget(GameLiving target, EEffect effectType, ESpellType spellType = ESpellType.Null)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<EcsGameEffect> effects);

                if (effects != null && spellType == ESpellType.Null)
                    return effects.FirstOrDefault();
                else if (effects != null)
                    return effects.OfType<EcsGameSpellEffect>().Where(e => e.SpellHandler.Spell.SpellType == spellType).FirstOrDefault();
                else
                    return null;
            }
        }

        public static EcsGameSpellEffect GetSpellEffectOnTarget(GameLiving target, EEffect effectType, ESpellType spellType = ESpellType.Null)
        {
            if (target == null)
                return null;

            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<EcsGameEffect> effects);

                if (effects != null)
                    return effects.OfType<EcsGameSpellEffect>().Where(e => e != null && (spellType == ESpellType.Null || e.SpellHandler.Spell.SpellType == spellType)).FirstOrDefault();
                else
                    return null;
            }
        }

        public static EcsGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, EEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<EcsGameEffect> effects);

                if (effects != null)
                    return (EcsGameAbilityEffect) effects.Where(e => e is EcsGameAbilityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static EcsImmunityEffect GetImmunityEffectOnTarget(GameLiving target, EEffect effectType)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(effectType, out List<EcsGameEffect> effects);

                if (effects != null)
                    return (EcsImmunityEffect) effects.Where(e => e is EcsImmunityEffect).FirstOrDefault();
                else
                    return null;
            }
        }

        public static EcsPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            lock (target.effectListComponent.EffectsLock)
            {
                target.effectListComponent.Effects.TryGetValue(EEffect.Pulse, out List<EcsGameEffect> effects);

                if (effects != null)
                    return (EcsPulseEffect) effects.Where(e => e is EcsPulseEffect && e.SpellHandler.Spell == spell).FirstOrDefault();
                else
                    return null;
            }
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, EEffect effectType)
        {
            if (target == null || target.effectListComponent == null)
                return false;

            EcsGameEffect effectToCancel;

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