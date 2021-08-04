using System;
using System.Collections.Generic;
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
                        case eEffect.BaseDex:
                        case eEffect.BaseCon:
                        case eEffect.Acuity:
                        case eEffect.StrCon:
                        case eEffect.DexQui:
                        case eEffect.BaseAf:
                        case eEffect.ArmorAbsorptionBuff:
                        case eEffect.BodyResistBuff:
                        case eEffect.SpiritResistBuff:
                        case eEffect.EnergyResistBuff:
                        case eEffect.HeatResistBuff:
                        case eEffect.ColdResistBuff:
                        case eEffect.MatterResistBuff:
                            HandlePropertyBuff(e, getPropertyFromEffect(e.EffectType));
                            break;
                            
                    }
                }
                EntityManager.RemoveEffect(e);
            }
        }

        private static void HandlePropertyBuff(ECSGameEffect e, List<eProperty> properties)
        {
            
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
                    foreach(var prop in properties)
                    {
                        Console.WriteLine($"Handling {prop.ToString()}");
                        e.Owner.AbilityBonus[(int)prop] += (int)e.SpellHandler.Spell.Value;
                    }
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
            foreach(var prop in getPropertyFromEffect(e.EffectType))
            {
                
                if (isPositiveEffect(e.EffectType))
                {
                    //if e.EffectType == buff, subtract value
                    e.Owner.AbilityBonus[(int)prop] -= (int)e.SpellHandler.Spell.Value;
                } else
                {
                    //else if e.EffectType == debuff, add value
                    e.Owner.AbilityBonus[(int)prop] += (int)e.SpellHandler.Spell.Value;
                }
                
            }
            
            if(e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
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

        private static List<eProperty> getPropertyFromEffect(eEffect e)
        {
            List<eProperty> list = new List<eProperty>();
            switch (e)
            {
                //stats
                case eEffect.BaseStr:
                case eEffect.StrengthDebuff:
                    list.Add(eProperty.Strength);
                    return list;
                case eEffect.BaseDex:
                case eEffect.DexterityDebuff:
                    list.Add(eProperty.Dexterity);
                    return list;
                case eEffect.BaseCon:
                case eEffect.ConstitutionDebuff:
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.Acuity:
                case eEffect.AcuityDebuff:
                    list.Add(eProperty.Acuity);
                    return list; 
                case eEffect.StrCon:
                case eEffect.StrConDebuff:
                    list.Add(eProperty.Strength);
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.DexQui:
                case eEffect.DexQuiDebuff:
                    list.Add(eProperty.Dexterity);
                    list.Add(eProperty.Quickness);
                    return list;
                case eEffect.BaseAf:
                case eEffect.ArmorFactorDebuff:
                    list.Add(eProperty.ArmorFactor);
                    return list;
                case eEffect.ArmorAbsorptionBuff:
                case eEffect.ArmorAbsorptionDebuff:
                    list.Add(eProperty.ArmorAbsorption);
                    return list;

                    //resists
                case eEffect.BodyResistBuff:
                case eEffect.BodyResistDebuff:
                    list.Add(eProperty.Resist_Body);
                    return list;
                case eEffect.SpiritResistBuff:
                case eEffect.SpiritResistDebuff:
                    list.Add(eProperty.Resist_Spirit);
                    return list;
                case eEffect.EnergyResistBuff:
                case eEffect.EnergyResistDebuff:
                    list.Add(eProperty.Resist_Energy);
                    return list;
                case eEffect.HeatResistBuff:
                case eEffect.HeatResistDebuff:
                    list.Add(eProperty.Resist_Heat);
                    return list;
                case eEffect.ColdResistBuff:
                case eEffect.ColdResistDebuff:
                    list.Add(eProperty.Resist_Cold);
                    return list;
                case eEffect.MatterResistBuff:
                case eEffect.MatterResistDebuff:
                    list.Add(eProperty.Resist_Matter);
                    return list;

                default:
                    return list;
            }
        }

        private static Boolean isPositiveEffect(eEffect e)
        {
            switch (e)
            {
                case eEffect.BaseStr:
                case eEffect.BaseDex:
                case eEffect.BaseCon:
                case eEffect.Acuity:
                case eEffect.StrCon:
                case eEffect.DexQui:
                case eEffect.BaseAf:
                case eEffect.ArmorAbsorptionBuff:
                case eEffect.BodyResistBuff:
                case eEffect.SpiritResistBuff:
                case eEffect.EnergyResistBuff:
                case eEffect.HeatResistBuff:
                case eEffect.ColdResistBuff:
                case eEffect.MatterResistBuff:
                    return true;

                case eEffect.StrengthDebuff:
                case eEffect.DexterityDebuff:
                case eEffect.ConstitutionDebuff:
                case eEffect.AcuityDebuff:
                case eEffect.StrConDebuff:
                case eEffect.DexQuiDebuff:
                case eEffect.ArmorFactorDebuff:
                case eEffect.ArmorAbsorptionDebuff:
                case eEffect.BodyResistDebuff:
                case eEffect.SpiritResistDebuff:
                case eEffect.EnergyResistDebuff:
                case eEffect.HeatResistDebuff:
                case eEffect.ColdResistDebuff:
                case eEffect.MatterResistDebuff:
                    return false;
                default:
                    return false;
            }
            

        }
        
    }
}