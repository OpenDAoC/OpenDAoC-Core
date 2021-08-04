using System;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.SpellEffects;

namespace DOL.GS
{
    public static class EffectService
    {
        public static void Tick(long tick)
        {
            foreach (var e in EntityManager.GetAllEffects())
            {
                if (e.CancelEffect)
                {
                    HandleCancelEffect(e);
                }
                else
                {
                    switch (e.EffectType)
                    {
                        case eEffect.BaseStr:
                            HandleBaseStat(e, eProperty.Strength);
                            break;
                        case eEffect.BaseDex:
                            HandleBaseStat(e, eProperty.Dexterity);
                            break;
                        case eEffect.BaseCon:
                            HandleBaseStat(e, eProperty.Constitution);
                            break;
                        case eEffect.Acuity:
                            HandleBaseStat(e, eProperty.Acuity);
                            break;
                        case eEffect.BaseAf:
                            HandleBaseStat(e, eProperty.ArmorFactor);
                            break;

                        case eEffect.DexQui:
                            HandleSpecStat(e, eProperty.Dexterity, eProperty.Quickness);
                            break;
                            
                    }
                }
                EntityManager.RemoveEffect(e);
            }
        }

        private static void HandleBaseStat(ECSGameEffect e, eProperty property)
        {
            Console.WriteLine($"Handling {property.ToString()}");
            if (e.Owner == null)
            {
                Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            EffectListComponent effectList = e.Owner.effectListComponent;
            if (effectList == null)
            {
                Console.WriteLine($"No effect list found for {e.Owner}");
                return;
            }


            if (!effectList.AddEffect(e))
            {
                SendSpellResistAnimation(e);

            }
            else
            {
                SendSpellAnimation(e);
                if (e.Owner is GamePlayer player)
                {
                    e.Owner.AbilityBonus[(int)property] += (int)e.SpellHandler.Spell.Value;
                    SendPlayerUpdates(player);
                }
            }
        }

        private static void HandleSpecStat(ECSGameEffect e, eProperty property1, eProperty property2)
        {
            Console.WriteLine($"Handling {property1.ToString()} and {property2.ToString()}");
            if (e.Owner == null)
            {
                Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            EffectListComponent effectList = e.Owner.effectListComponent;
            if (effectList == null)
            {
                Console.WriteLine($"No effect list found for {e.Owner}");
                return;
            }


            if (!effectList.AddEffect(e))
            {
                SendSpellResistAnimation(e);

            }
            else
            {
                SendSpellAnimation(e);
                if (e.Owner is GamePlayer player)
                {
                    e.Owner.AbilityBonus[(int)property1] += (int)e.SpellHandler.Spell.Value;
                    e.Owner.AbilityBonus[(int)property2] += (int)e.SpellHandler.Spell.Value;
                    SendPlayerUpdates(player);
                }
            }
        }

        private static void SendPlayerUpdates(GamePlayer player)
        {
            player.Out.SendCharStatsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
        }

        //todo - abstract this out to dynamically cancel the effect. Need a way to look up eProperty and such
        private static void HandleCancelEffect(ECSGameEffect e)
        {
            Console.WriteLine($"Handling Cancel Effect");
            if (!e.Owner.effectListComponent.RemoveEffect(e))
            {
                Console.WriteLine("Unable to remove effect!");
                return;
            }
            
            e.Owner.AbilityBonus[(int)eProperty.Constitution] -= (int)e.SpellHandler.Spell.Value;
            if(e.Owner is GamePlayer player)
            {
                player.Out.SendCharStatsUpdate();
                player.UpdateEncumberance();
                player.UpdatePlayerStatus();
                player.Out.SendUpdatePlayer();
                //Now update EffectList
                player.Out.SendUpdateIcons(e.Owner.effectListComponent.Effects.Values.ToList(), ref e.Owner.effectListComponent._lastUpdateEffectsCount);
            } 
        }

        private static void SendSpellAnimation(ECSGameEffect e)
        {
            foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.SpellHandler.Target, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
            }

            if (e.Owner is GamePlayer player1)
            {
                player1.Out.SendUpdateIcons(player1.effectListComponent.Effects.Values.ToList(), ref player1.effectListComponent._lastUpdateEffectsCount);
            }
        }

        private static void SendSpellResistAnimation(ECSGameEffect e)
        {
            foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.SpellHandler.Target, e.SpellHandler.Spell.ClientEffect, 0, false, 0);
            }
        }

        
    }
}