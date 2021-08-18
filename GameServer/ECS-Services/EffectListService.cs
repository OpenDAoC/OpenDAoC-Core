using DOL.GS.Spells;
using System.Collections.Generic;
using System;
using System.Numerics;

namespace DOL.GS
{
    public static class EffectListService
    {
        public static void Tick(long tick)
        {
            foreach (var p in EntityManager.GetAllPlayers())
            {
                HandleEffects(tick, p);
            }
            foreach (var npc in EntityManager.GetAllNpcs())
            {
                HandleEffects(tick, (GameLiving)npc);
            }
        }

        private static void HandleEffects(long tick, GameLiving living)
        {
            if (living.effectListComponent.Effects.Count > 0)
            {
                foreach (var effect in living.effectListComponent.Effects)
                {
                    if (!effect.Value.Owner.IsAlive)
                    {
                        effect.Value.CancelEffect = true;
                        EntityManager.AddEffect(effect.Value);
                        continue;
                    }

                    if (tick > effect.Value.ExpireTick && !effect.Value.SpellHandler.Spell.IsConcentration)
                    {
                        if (effect.Value.EffectType == eEffect.Pulse && effect.Value.SpellHandler.Caster.LastPulseCast == effect.Value.SpellHandler.Spell)
                        {
                            if (effect.Value.SpellHandler.Spell.IsHarmful)
                            {
                                ((SpellHandler)effect.Value.SpellHandler).SendCastAnimation();

                            }
                            effect.Value.SpellHandler.StartSpell(null);
                        }
                        else
                        {
                            if (effect.Value.EffectType == eEffect.Bleed)
                                effect.Value.Owner.TempProperties.removeProperty(StyleBleeding.BLEED_VALUE_PROPERTY);

                            effect.Value.CancelEffect = true;
                            EntityManager.AddEffect(effect.Value);
                        }
                    }
                    if (effect.Value.EffectType == eEffect.DamageOverTime || effect.Value.EffectType == eEffect.Bleed)
                    {
                        if (effect.Value.LastTick == 0)
                        {
                            EffectService.OnEffectPulse(effect.Value);
                            effect.Value.LastTick = GameLoop.GameLoopTime;
                        }
                        else if (tick > effect.Value.PulseFreq + effect.Value.LastTick)
                        {
                            EffectService.OnEffectPulse(effect.Value);
                            effect.Value.LastTick += effect.Value.PulseFreq;
                        }
//=======
//{
//                EffectListComponent effectListComponent = p.effectListComponent;
//                foreach(ECSGameEffect effect in effectListComponent.Effects.Values)
//                {
//                    if(effect.ExpireTick != 0 && effect.ExpireTick <= tick)
//                    {
//                        effect.CancelEffect = true;
//                        Console.WriteLine($"Canceling effect {effect}");
//                        EntityManager.AddEffect(effect);
//                    }

//                    if(effect.Duration > 1 && effect.ExpireTick == 0)
//                    {
//                        effect.ExpireTick = tick + effect.Duration;
//                        Console.WriteLine($"Current tick {tick}. Duration {effect.Duration}. Expiry tick {effect.ExpireTick}");
//                    }
//                }
//            }

//            foreach (var n in EntityManager.GetAllNpcs())
//            {
//                EffectListComponent effectListComponent = n.effectListComponent;
//                foreach (ECSGameEffect effect in effectListComponent.Effects.Values)
//                {
//                    if (effect.ExpireTick != 0 && effect.ExpireTick <= tick)
//                    {
//                        effect.CancelEffect = true;
//                        Console.WriteLine($"Canceling effect {effect}");
//                        EntityManager.AddEffect(effect);
//                    }

//                    if (effect.Duration > 1 && effect.ExpireTick == 0)
//                    {
//                        effect.ExpireTick = tick + effect.Duration;
//                        Console.WriteLine($"Current tick {tick}. Duration {effect.Duration}. Expiry tick {effect.ExpireTick}");
//>>>>>>> CombinedGameLoop
                    }
                }
            }
        }
    }
}