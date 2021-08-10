using System;
using System.Numerics;

namespace DOL.GS
{
    public class EffectListService
    {
        public static void Tick(long tick)
        {
            foreach (var p in EntityManager.GetAllPlayers())
{
                EffectListComponent effectListComponent = p.effectListComponent;
                foreach(ECSGameEffect effect in effectListComponent.Effects.Values)
                {
                    if(effect.ExpireTick != 0 && effect.ExpireTick <= tick)
                    {
                        effect.CancelEffect = true;
                        Console.WriteLine($"Canceling effect {effect}");
                        EntityManager.AddEffect(effect);
                    }

                    if(effect.Duration > 1 && effect.ExpireTick == 0)
                    {
                        effect.ExpireTick = tick + effect.Duration;
                        Console.WriteLine($"Current tick {tick}. Duration {effect.Duration}. Expiry tick {effect.ExpireTick}");
                    }
                }
            }

            foreach (var n in EntityManager.GetAllNpcs())
            {
                EffectListComponent effectListComponent = n.effectListComponent;
                foreach (ECSGameEffect effect in effectListComponent.Effects.Values)
                {
                    if (effect.ExpireTick != 0 && effect.ExpireTick <= tick)
                    {
                        effect.CancelEffect = true;
                        Console.WriteLine($"Canceling effect {effect}");
                        EntityManager.AddEffect(effect);
                    }

                    if (effect.Duration > 1 && effect.ExpireTick == 0)
                    {
                        effect.ExpireTick = tick + effect.Duration;
                        Console.WriteLine($"Current tick {tick}. Duration {effect.Duration}. Expiry tick {effect.ExpireTick}");
                    }
                }
            }
        }
    }
}