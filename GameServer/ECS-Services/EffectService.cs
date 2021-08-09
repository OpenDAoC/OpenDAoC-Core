using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.SpellEffects;
using DOL.GS.Spells;

namespace DOL.GS
{
    public static class EffectService
    {
        public static void Tick(long tick)
        {
            foreach (var e in EntityManager.GetAllEffects())
            {
                if (e.CancelEffect)// &&  )
                {
                    if (tick > e.ExpireTick)
                        HandleCancelEffect(e);
                }
                else
                {

                    
                    //switch (e.EffectType)
                    //{
                        
                        //case eEffect.BaseStr:
                        //case eEffect.BaseDex:
                        //case eEffect.BaseCon:
                        //case eEffect.Acuity:
                        //case eEffect.StrCon:
                        //case eEffect.DexQui:
                        //case eEffect.BaseAf:
                        //case eEffect.ArmorAbsorptionBuff:
                        //case eEffect.BodyResistBuff:
                        //case eEffect.SpiritResistBuff:
                        //case eEffect.EnergyResistBuff:
                        //case eEffect.HeatResistBuff:
                        //case eEffect.ColdResistBuff:
                        //case eEffect.MatterResistBuff:
                        //case eEffect.HealOverTime:
                        //case eEffect.DamageAdd:
                            HandlePropertyBuff(e, getPropertyFromEffect(e.EffectType));
                            //break;
                            
                    //}
                }
                
                
                EntityManager.RemoveEffect(e);
            }
        }

        private static void HandlePropertyBuff(ECSGameEffect e, List<eProperty> properties)
        {
            if (e.Owner == null /*&& !e.SpellHandler.Spell.Target.ToLower().Equals("group") && !e.SpellHandler.Spell.Target.ToLower().Equals("self")*/)
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
                    
                foreach (var prop in properties)
                {
                    Console.WriteLine($"Handling {prop.ToString()}");
                    if (isPositiveEffect(e.EffectType))
                        e.Owner.AbilityBonus[(int)prop] += (int)e.SpellHandler.Spell.Value;
                    else
                       // e.Owner.AbilityBonus[(int)prop] -= (int)e.SpellHandler.Spell.Value;
                        e.Owner.DebuffCategory[(int)prop] += (int)e.SpellHandler.Spell.Value;
                }


                if (e.Owner is GamePlayer player)
                {
                    SendPlayerUpdates(player);
                }
            }
        }

        private static void SendPlayerUpdates(GamePlayer player)
        {
            player.Out.SendCharStatsUpdate();
            player.Out.SendCharResistsUpdate();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
        }

        //todo - abstract this out to dynamically cancel the effect. Need a way to look up eProperty and such
        private static void HandleCancelEffect(ECSGameEffect e)
        {

            Console.WriteLine($"Handling Cancel Effect: " + e.EffectType.ToString());
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
                    //e.Owner.AbilityBonus[(int)prop] += (int)e.SpellHandler.Spell.Value;
                    e.Owner.DebuffCategory[(int)prop] -= (int)e.SpellHandler.Spell.Value;
                }
                
            }
            
            if(e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
                //Now update EffectList
                player.Out.SendUpdateIcons(new List<ECSGameEffect>() { e }/*e.Owner.effectListComponent.Effects.Values.ToList()*/, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
            } 
        }

        private static void SendSpellAnimation(ECSGameEffect e)
        {
            GameLiving target = e.SpellHandler.Target != null ? e.SpellHandler.Target : e.SpellHandler.Caster;

            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, /*e.SpellHandler.Target target*/e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
            }

            if (e.Owner is GamePlayer player1)
            {
                player1.Out.SendUpdateIcons(new List<ECSGameEffect>() { e }/*player1.effectListComponent.Effects.Values.ToList()*/, ref player1.effectListComponent._lastUpdateEffectsCount);
            }
        }

        private static void SendSpellResistAnimation(ECSGameEffect e)
        {
            GameLiving target = e.SpellHandler.Target != null ? e.SpellHandler.Target : e.SpellHandler.Caster;
            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, /*e.SpellHandler.Target target*/e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 0);
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
                case eEffect.Bladeturn:
                case eEffect.DamageAdd:
                case eEffect.DamageReturn:
                case eEffect.FocusShield:
                case eEffect.AblativeArmor:
                case eEffect.MeleeDamageBuff:
                case eEffect.MeleeHasteBuff:
                case eEffect.Celerity:
                case eEffect.MovementSpeedBuff:
                case eEffect.HealOverTime:

                case eEffect.BaseStr:
                case eEffect.BaseDex:
                case eEffect.BaseCon:
                case eEffect.Acuity:
                case eEffect.StrCon:
                case eEffect.DexQui:
                case eEffect.BaseAf:
                case eEffect.SpecAf:
                case eEffect.ArmorAbsorptionBuff:

                case eEffect.BodyResistBuff:
                case eEffect.SpiritResistBuff:
                case eEffect.EnergyResistBuff:
                case eEffect.HeatResistBuff:
                case eEffect.ColdResistBuff:
                case eEffect.MatterResistBuff:

                case eEffect.HealthRegenBuff:
                case eEffect.EnduranceRegenBuff:
                case eEffect.PowerRegenBuff:
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

        public static void OnEffectPulse(ECSGameEffect effect)
        {

            if (effect.Owner.IsAlive == false)
            {
                //effect.Cancel(false);
                effect.CancelEffect = true;
                EntityManager.AddEffect(effect);
            }
        
            if (effect.Owner.IsAlive)
            {
                // An acidic cloud surrounds you!
                (effect.SpellHandler as DoTSpellHandler).MessageToLiving(effect.Owner, effect.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                // {0} is surrounded by an acidic cloud!
                Message.SystemToArea(effect.Owner, Util.MakeSentence(effect.SpellHandler.Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
                (effect.SpellHandler as DoTSpellHandler).OnDirectEffect(effect.Owner, effect.Effectiveness);
            }
        }

        public static void OnDirectEffect(GameLiving target, double effectiveness, ECSGameEffect effect)
        {
            if (target == null) return;
            if (!target.IsAlive || target.ObjectState != GameLiving.eObjectState.Active) return;

            // no interrupts on DoT direct effect
            // calc damage
            AttackData ad = (effect.SpellHandler as DoTSpellHandler).CalculateDamageToTarget(target, effectiveness);
            (effect.SpellHandler as DoTSpellHandler).SendDamageMessages(ad);
            (effect.SpellHandler as DoTSpellHandler).DamageTarget(ad, false);
        }
    }
}