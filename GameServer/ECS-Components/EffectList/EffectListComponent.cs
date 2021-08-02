using System;
using System.Collections.Generic;

namespace DOL.GS
{
    //Component for holding persistent Effects on the player//
    
    public class EffectListComponent
    {
        public long _lastChecked = 0;
        public GamePlayer Owner;
        
        private object _effectsLock = new object(); 
        public Dictionary<eEffect,IECSGameEffect> Effects = new Dictionary<eEffect, IECSGameEffect>();

        public EffectListComponent(GamePlayer p)
        {
            Owner = p;
        }

        public bool AddEffect(IECSGameEffect effect)
        {
            lock (_effectsLock)
            {
                if (Effects.ContainsKey(effect.Type))
                {
                    Console.WriteLine("Effect List contains type");
                    return false;
                }
                else
                {
                    Effects.Add(effect.Type,effect);
                }
                return true;
            }
        }

        public bool RemoveEffect(IECSGameEffect effect)
        {
            lock (_effectsLock)
            {
                if (!Effects.ContainsKey(effect.Type))
                {
                    Console.WriteLine("Effect List does not contain type");
                    return false;
                }
                else
                {
                    Effects.Remove(effect.Type);
                }
                return true;
            }
        }


    }
}