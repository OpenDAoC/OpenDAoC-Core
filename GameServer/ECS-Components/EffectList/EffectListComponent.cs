using System;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    //Component for holding persistent Effects on the player//
    
    public class EffectListComponent
    {
        public long _lastChecked = 0;
        public GameLiving Owner;
        
        public int _lastUpdateEffectsCount = 0;
        
        private object _effectsLock = new object(); 
        public Dictionary<eEffect, List<ECSGameEffect>> Effects = new Dictionary<eEffect, List<ECSGameEffect>>();
        public Dictionary<int, ECSGameEffect> EffectIdToEffect = new Dictionary<int, ECSGameEffect>();

        public EffectListComponent(GameLiving p)
        {
            Owner = p;
        }

        public bool AddEffect(ECSGameEffect effect)
        {
            lock (_effectsLock)
            {
                try
                {
                    if (Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                    {
                        // Effects contains this effect already so refresh it
                        if (existingEffects.Where(e => e.SpellHandler.Spell.ID == effect.SpellHandler.Spell.ID).FirstOrDefault() != null)
                        {
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                // If this buff is stronger > in list. cancel current buff and add this one- Return true;
                                if (existingEffects[i].SpellHandler.Spell.IsPulsing && effect.SpellHandler.Caster.LastPulseCast == effect.SpellHandler.Spell
                                    && existingEffects[i].SpellHandler.Spell.ID == effect.SpellHandler.Spell.ID
                                    || (existingEffects[i].SpellHandler.Spell.IsConcentration && effect == existingEffects[i])
                                    || existingEffects[i].SpellHandler.Spell.ID == effect.SpellHandler.Spell.ID)
                                   
                                {
                                    effect.IsDisabled = existingEffects[i].IsDisabled;
                                    effect.IsBuffActive = existingEffects[i].IsBuffActive;
                                    
                                    if (effect.SpellHandler.Spell.IsPulsing)
                                        effect.RenewEffect = true;

                                    Effects[effect.EffectType][i] = effect;
                                    if (!effect.IsDisabled && !effect.IsBuffActive)
                                        EntityManager.AddEffect(effect);
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            bool addEffect = false;
                            // Check to see if we can add new Effect
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                // Better Effect so disable the current Effect
                                if (effect.SpellHandler.Spell.Value > existingEffects[i].SpellHandler.Spell.Value)
                                {
                                    EffectService.RequestDisableEffect(existingEffects[i], true);
                                    addEffect = true;
                                    //existingEffects.Add(effect);
                                }
                                else if (effect.SpellHandler.Spell.Value < existingEffects[i].SpellHandler.Spell.Value)
                                {
                                    //effect.IsDisabled = true;
                                    EffectService.RequestDisableEffect(effect, true);
                                    addEffect = true;
                                }
                                else if ((effect.SpellHandler.Spell.Group != existingEffects[i].SpellHandler.Spell.Group) 
                                   /* && (effect.SpellHandler.Spell.EffectGroup == 0 || effect.SpellHandler.Spell.EffectGroup != existingEffects[i].SpellHandler.Spell.EffectGroup)*/)
                                {
                                    addEffect = true;
                                    //existingEffects.Add(effect);
                                }
                                else if (effect.EffectType == eEffect.DamageAdd)
                                    addEffect = true;
                            }
                            if (addEffect)
                            {
                                existingEffects.Add(effect);
                                return true;
                            }
                        }
                        Console.WriteLine("Effect List contains type: " + effect.EffectType.ToString() + " (" + effect.Owner.Name + ")");
                        return false;
                    }
                    else if (Effects.ContainsKey(effect.EffectType))
                    {
                        Effects[effect.EffectType].Add(effect);
                    }
                    else
                    {                      
                        Effects.Add(effect.EffectType, new List<ECSGameEffect> { effect });
                        if (effect.EffectType != eEffect.Pulse && effect.Icon != 0)
                            EffectIdToEffect.Add(effect.Icon, effect);

                        if (Effects.Count == 1 && !EntityManager.GetLivingByComponent(typeof(EffectListComponent)).ToArray().Contains(Owner))
                        {
                            EntityManager.AddComponent(typeof(EffectListComponent), Owner);
                        }
                    }
                    
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error adding an Effect {e}");
                    return false;
                }
                
            }
            
        }

        public List<ECSGameEffect> GetAllEffects()
        {
            lock (_effectsLock)
            {
                var temp = new List<ECSGameEffect>();
                foreach (var effects in Effects.Values)
                    foreach (var effect in effects)
                        if (effect.EffectType != eEffect.Pulse)
                            temp.Add(effect);

                return temp.OrderBy(e => e.StartTick).ToList();
            }
        }

        public ECSGameEffect TryGetEffectFromEffectId(int effectId)
        {
            lock (_effectsLock)
            {
                EffectIdToEffect.TryGetValue(effectId, out var effect);
                return effect;
            }
        }

        public bool RemoveEffect(ECSGameEffect effect)
        {
            lock (_effectsLock)
            {
                try
                {
                    if (!Effects.ContainsKey(effect.EffectType))
                    {
                        Console.WriteLine("Effect List does not contain type: " + effect.EffectType.ToString());
                        return false;
                    }
                    else
                    {
                        if (effect.CancelEffect)
                        {
                            Effects[effect.EffectType].Remove(effect);
                            EffectIdToEffect.Remove(effect.Icon);

                            if (Effects[effect.EffectType].Count > 0)
                            {
                                if (Effects[effect.EffectType].FirstOrDefault().IsDisabled)
                                    EffectService.RequestDisableEffect(Effects[effect.EffectType].FirstOrDefault(), false);
                                //foreach (var eff in Effects[effect.EffectType])
                                //EffectService.RequestDisableEffect()
                            }
                            else
                            {
                                EffectIdToEffect.Remove(effect.Icon);
                                Effects.Remove(effect.EffectType);
                            }

                            if (Effects.Count == 0)
                            {
                                EntityManager.RemoveComponent(typeof(EffectListComponent), Owner);
                            }
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error removing an Effect from EffectList {e}");
                    return false;
                }

            }
        }

        public bool ContainsEffectForEffectType(eEffect effectType)
        {
            lock (_effectsLock)
            {
                try
                {
                    if (Effects.ContainsKey(effectType))
                    {
                        return true;
                    } else
                    {
                        return false;
                    }
                } catch (Exception e)
                {
                    Console.WriteLine($"Error attempting to check effect type");
                    return false;
                }
            }
        }


    }
}