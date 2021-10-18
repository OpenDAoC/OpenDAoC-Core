using DOL.GS.Spells;
using System.Collections.Generic;
using System;
using System.Numerics;
using ECS.Debug;
using System.Linq;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public static class EffectListService
    {
        private const string ServiceName = "EffectListService";

        static EffectListService()
        {
            //This should technically be the world manager
            EntityManager.AddService(typeof(EffectListService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var living in EntityManager.GetLivingByComponent(typeof(EffectListComponent)))
            {
                HandleEffects(tick, living);
            }

            Diagnostics.StopPerfCounter(ServiceName);               
        }

        private static void HandleEffects(long tick, GameLiving living)
        {
            if (living?.effectListComponent?.Effects.Count > 0)
            {
                foreach (var effects in living.effectListComponent.Effects.Values)
                {
                    foreach (var effect in effects)
                    {
                        if (!effect.Owner.IsAlive)
                        {
                            EffectService.RequestCancelEffect(effect);
                            continue;
                        }

                        // TEMP - A lot of the code below assumes effects come from spells but many effects come from abilities (Sprint, Stealth, RAs, etc)
                        // This will need a better refactor later but for now this prevents crashing while working on porting over non-spell based effects to our system.
                        if (!effect.FromSpell)
                            continue;

                        if (tick > effect.ExpireTick && !effect.SpellHandler.Spell.IsConcentration)
                        {
                            if (effect.EffectType == eEffect.Pulse && effect.SpellHandler.Caster.LastPulseCast == effect.SpellHandler.Spell)
                            {
                                

                                if (effect.SpellHandler.Spell.PulsePower > 0)
                                {
                                    if (effect.SpellHandler.Caster.Mana >= effect.SpellHandler.Spell.PulsePower)
                                    {
                                        effect.SpellHandler.Caster.Mana -= effect.SpellHandler.Spell.PulsePower;
                                        //if (Spell.InstrumentRequirement != 0 || !HasPositiveEffect)
                                        //{
                                        //    SendEffectAnimation(Caster, 0, true, 1); // pulsing auras or songs
                                        //}

                                        if (effect.SpellHandler.Spell.IsHarmful && effect.SpellHandler.Spell.SpellType != (byte)eSpellType.Charm && effect.SpellHandler.Spell.SpellType != (byte)eSpellType.SpeedDecrease)
                                        {
                                            if (!(effect.Owner.IsMezzed || effect.Owner.IsStunned))
                                                ((SpellHandler)effect.SpellHandler).SendCastAnimation();

                                        }
                                        else if (effect.SpellHandler.Spell.SpellType == (byte)eSpellType.Charm)
                                        {
                                            ((CharmSpellHandler)effect.SpellHandler).SendEffectAnimation(effect.SpellHandler.GetTarget(), 0, false, 1);
                                        }
                                        else if (effect.SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                                        {
                                            ((SpeedDecreaseSpellHandler)effect.SpellHandler).SendEffectAnimation(effect.SpellHandler.GetTarget(), 0, false, 1);
                                        }
                                        effect.SpellHandler.StartSpell(null);
                                        effect.ExpireTick += effect.PulseFreq;
                                    }
                                    else
                                    {
                                        ((SpellHandler)effect.SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                        EffectService.RequestCancelConcEffect(effect);
                                    }
                                }
                                else
                                {
                                    effect.SpellHandler.StartSpell(null);
                                    effect.ExpireTick += effect.PulseFreq;
                                }
                            }
                            else
                            {
                                if (effect.EffectType == eEffect.Bleed)
                                    effect.Owner.TempProperties.removeProperty(StyleBleeding.BLEED_VALUE_PROPERTY);

                                if (effect.SpellHandler.Spell.IsPulsing && effect.SpellHandler.Caster.LastPulseCast == effect.SpellHandler.Spell &&
                                    effect.ExpireTick >= (effect.LastTick + (effect.Duration > 0 ? effect.Duration : effect.PulseFreq)))
                                {
                                    //Add time to effect to make sure the spell refreshes instead of cancels
                                    effect.ExpireTick += GameLoop.TickRate;
                                    effect.LastTick = GameLoop.GameLoopTime;
                                }
                                else
                                {
                                    EffectService.RequestCancelEffect(effect);
                                }
                            }
                        }
                        if (effect.EffectType == eEffect.DamageOverTime || effect.EffectType == eEffect.Bleed)
                        {
                            // Initial DoT application
                            if (effect.LastTick == 0)
                            {
                                // Remove stealth on first application since the code that normally handles removing stealth on
                                // attack ignores DoT damage, since only the first tick of a DoT should remove stealth.
                                GamePlayer ownerPlayer = effect.Owner as GamePlayer;
                                if (ownerPlayer != null)
                                    ownerPlayer.Stealth(false);

                                EffectService.OnEffectPulse(effect);
                                effect.LastTick = GameLoop.GameLoopTime;
                            }
                            // Subsequent DoT ticks
                            else if (tick > effect.PulseFreq + effect.LastTick)
                            {
                                EffectService.OnEffectPulse(effect);
                                effect.LastTick += effect.PulseFreq;
                            }
                        }
                        if (!(effect is ECSImmunityEffect) && effect.EffectType != eEffect.Pulse && effect.SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                        {
                            if (tick > effect.NextTick)
                            {
                                double factor = 2.0 - (effect.Duration - effect.GetRemainingTimeForClient()) / (double)(effect.Duration >> 1);
                                if (factor < 0) factor = 0;
                                else if (factor > 1) factor = 1;

                                //effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.SpellHandler.Spell.ID, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);
                                effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.EffectType, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);

                                UnbreakableSpeedDecreaseSpellHandler.SendUpdates(effect.Owner);
                                effect.NextTick += effect.TickInterval;
                                if (factor <= 0)
                                    effect.ExpireTick = GameLoop.GameLoopTime - 1;
                            }
                        }
                        if (effect.NextTick != 0 && tick > effect.NextTick)
                        {
                            effect.OnEffectPulse();
                        }
                        if (effect.SpellHandler.Spell.SpellType == (byte)eSpellType.HealOverTime && tick > effect.NextTick)
                        {
                            (effect.SpellHandler as HoTSpellHandler).OnDirectEffect(effect.Owner, effect.Effectiveness);
                            effect.NextTick += effect.PulseFreq;
                        }
                        if (effect.SpellHandler.Spell.IsConcentration && tick > effect.NextTick)
                        {
                            if (!effect.SpellHandler.Caster.
                                IsWithinRadius(effect.Owner,
                                effect.SpellHandler.Spell.SpellType != (byte)eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : effect.SpellHandler.Spell.Range)
                                && !effect.IsDisabled)
                            {
                                EffectService.RequestDisableEffect(effect, true);
                            }
                            else if (effect.SpellHandler.Caster.IsWithinRadius(effect.Owner,
                                effect.SpellHandler.Spell.SpellType != (byte)eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : effect.SpellHandler.Spell.Range)
                                && effect.IsDisabled)
                            {
                                List<ECSGameEffect> concEffects;
                                effect.Owner.effectListComponent.Effects.TryGetValue(effect.EffectType, out concEffects);
                                bool isBest = false;
                                if (concEffects.Count == 1)
                                    isBest = true;
                                else if (concEffects.Count > 1)
                                {
                                    foreach (var eff in effects)
                                        if (effect.SpellHandler.Spell.Value > eff.SpellHandler.Spell.Value)
                                        {
                                            isBest = true;
                                            break;
                                        }
                                        else
                                            isBest = false;
                                }
                                
                                if (isBest)
                                    EffectService.RequestDisableEffect(effect, false);
                            }

                            effect.NextTick += effect.PulseFreq;
                        }                    
                    }
                }
            }
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Null)
        {
            List<ECSGameEffect> effects;
            target.effectListComponent.Effects.TryGetValue(effectType, out effects);

            if (effects != null && spellType == eSpellType.Null)
                return effects.FirstOrDefault();
            else if (effects != null)
                return effects.Where(e => e.SpellHandler.Spell.SpellType == (byte)spellType).FirstOrDefault();
            else
                return null;
        }
    }
}