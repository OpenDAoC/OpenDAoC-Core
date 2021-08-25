using System;
using System.Collections.Generic;

namespace DOL.GS
{
    //Component for holding persistent Effects on the player//
    
    public class EffectListComponent
    {
        public long _lastChecked = 0;
        public GameLiving Owner;
        
        public int _lastUpdateEffectsCount = 0;
        
        private object _effectsLock = new object(); 
        public Dictionary<eEffect,ECSGameEffect> Effects = new Dictionary<eEffect, ECSGameEffect>();
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
                    if (Effects.TryGetValue(effect.EffectType, out ECSGameEffect existing))
                    {
                        //If this buff is stronger > in list. cancel current buff and add this one- Return true;
                        if (existing.SpellHandler.Spell.IsPulsing)
                        {
                            Effects[effect.EffectType] = effect;
                            effect.RenewEffect = true;
                            return true;
                        }
                        Console.WriteLine("Effect List contains type: " + effect.EffectType.ToString() + " (" + effect.Owner.Name + ")");
                        return false;
                    }
                    else
                    {                      
                        Effects.Add(effect.EffectType, effect);
                        if (effect.EffectType != eEffect.Pulse && effect.Icon != 0)
                            EffectIdToEffect.Add(effect.Icon, effect);

                        if (Effects.Count == 1)
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
                        EffectIdToEffect.Remove(effect.Icon);
                        Effects.Remove(effect.EffectType);
                        if (Effects.Count == 0)
                        {
                            EntityManager.RemoveComponent(typeof(EffectListComponent), Owner);
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


    }
}