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
            else if (e.EffectType != eEffect.Pulse)
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
            foreach (var prop in getPropertyFromEffect(e.EffectType))
            {

                if (isPositiveEffect(e.EffectType))
                {
                    //if e.EffectType == buff, subtract value
                    e.Owner.AbilityBonus[(int)prop] -= (int)e.SpellHandler.Spell.Value;
                }
                else
                {
                    //else if e.EffectType == debuff, add value
                    //e.Owner.AbilityBonus[(int)prop] += (int)e.SpellHandler.Spell.Value;
                    e.Owner.DebuffCategory[(int)prop] -= (int)e.SpellHandler.Spell.Value;
                }

            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
                //Now update EffectList
                player.Out.SendUpdateIcons(/*new List<ECSGameEffect>() { e }*/e.Owner.effectListComponent.Effects.Values.Where(ef => ef.Icon != 0).ToList(), ref e.Owner.effectListComponent._lastUpdateEffectsCount);
            }
        }

        public static void SendSpellAnimation(ECSGameEffect e)
        {
            GameLiving target = e.SpellHandler.Target != null ? e.SpellHandler.Target : e.SpellHandler.Caster;

            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, /*e.SpellHandler.Target target*/e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
            }

            if (e.Owner is GamePlayer player1)
            {
                player1.Out.SendUpdateIcons(/*new List<ECSGameEffect>() { e }*/player1.effectListComponent.Effects.Values.Where(ef => ef.Icon != 0).ToList(), ref player1.effectListComponent._lastUpdateEffectsCount);
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
                if (effect.SpellHandler is DoTSpellHandler handler)
                {
                    // An acidic cloud surrounds you!
                    handler.MessageToLiving(effect.Owner, effect.SpellHandler.Spell.Message1, eChatType.CT_Spell);
                    // {0} is surrounded by an acidic cloud!
                    Message.SystemToArea(effect.Owner, Util.MakeSentence(effect.SpellHandler.Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);
                    handler.OnDirectEffect(effect.Owner, effect.Effectiveness);
                }
                else if (effect.SpellHandler is StyleBleeding bleedHandler)
                {
                    if (effect.StartTick + effect.PulseFreq > GameLoop.GameLoopTime && effect.Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY) == 0)
                    {
                        effect.Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, (int)bleedHandler.Spell.Damage + (int)bleedHandler.Spell.Damage * Util.Random(25) / 100);  // + random max 25%

                    }
                    bleedHandler.MessageToLiving(effect.Owner, bleedHandler.Spell.Message1, eChatType.CT_YouWereHit);
                    Message.SystemToArea(effect.Owner, Util.MakeSentence(bleedHandler.Spell.Message2, effect.Owner.GetName(0, false)), eChatType.CT_YouHit, effect.Owner);

                    int bleedValue = effect.Owner.TempProperties.getProperty<int>(StyleBleeding.BLEED_VALUE_PROPERTY);

                    AttackData ad = bleedHandler.CalculateDamageToTarget(effect.Owner, 1.0);

                    bleedHandler.SendDamageMessages(ad);

                    // attacker must be null, attack result is 0x0A
                    foreach (GamePlayer player in ad.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    {
                        player.Out.SendCombatAnimation(null, ad.Target, 0, 0, 0, 0, 0x0A, ad.Target.HealthPercent);
                    }
                    // send animation before dealing damage else dead livings show no animation
                    ad.Target.OnAttackedByEnemy(ad);
                    ad.Attacker.DealDamage(ad);

                    if (--bleedValue <= 0 || !effect.Owner.IsAlive)
                    {
                        //effect.Cancel(false);
                        effect.ExpireTick = GameLoop.GameLoopTime - 1;
                        //effect.CancelEffect = true;
                        //EntityManager.AddEffect(effect);
                    }
                    else effect.Owner.TempProperties.setProperty(StyleBleeding.BLEED_VALUE_PROPERTY, bleedValue);
                }
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
        public static eEffect MapEffect(SpellHandler spellHandler)
        {
            //Console.WriteLine("Spell of type: " + ((eSpellType)spellHandler.Spell.SpellType).ToString());
            switch (spellHandler.Spell.SpellType)
            {
                #region Positive Effects
                //positive effects
                case (byte)eSpellType.Bladeturn:
                    return eEffect.Bladeturn;
                case (byte)eSpellType.DamageAdd:
                    return eEffect.DamageAdd;
                //case (byte)eSpellType.DamageReturn:
                //    return eEffect.DamageReturn;
                case (byte)eSpellType.DamageShield: //FocusShield: Could be the wrong SpellType here
                    return eEffect.FocusShield;
                case (byte)eSpellType.AblativeArmor:
                    return eEffect.AblativeArmor;
                case (byte)eSpellType.MeleeDamageBuff:
                    return eEffect.MeleeDamageBuff;
                case (byte)eSpellType.CombatSpeedBuff:
                    return eEffect.MeleeHasteBuff;
                //case (byte)eSpellType.Celerity:  //Possibly the same as CombatSpeedBuff?
                //    return eEffect.Celerity;
                case (byte)eSpellType.SpeedEnhancement:
                    return eEffect.MovementSpeedBuff;
                case (byte)eSpellType.HealOverTime:
                    return eEffect.HealOverTime;

                //stats
                case (byte)eSpellType.StrengthBuff:
                    return eEffect.BaseStr;
                case (byte)eSpellType.DexterityBuff:
                    return eEffect.BaseDex;
                case (byte)eSpellType.ConstitutionBuff:
                    return eEffect.BaseCon;
                case (byte)eSpellType.StrengthConstitutionBuff:
                    return eEffect.StrCon;
                case (byte)eSpellType.DexterityQuicknessBuff:
                    return eEffect.DexQui;
                case (byte)eSpellType.AcuityBuff:
                    return eEffect.Acuity;
                case (byte)eSpellType.ArmorAbsorptionBuff:
                    return eEffect.ArmorAbsorptionBuff;
                case (byte)eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;
                case (byte)eSpellType.ArmorFactorBuff:
                    if (spellHandler.SpellLine.IsBaseLine)
                        return eEffect.BaseAf; //currently no map to specAF. where is spec AF handled?
                    else
                        return eEffect.SpecAf;


                //resists
                case (byte)eSpellType.BodyResistBuff:
                    return eEffect.BodyResistBuff;
                case (byte)eSpellType.SpiritResistBuff:
                    return eEffect.SpiritResistBuff;
                case (byte)eSpellType.EnergyResistBuff:
                    return eEffect.EnergyResistBuff;
                case (byte)eSpellType.HeatResistBuff:
                    return eEffect.HeatResistBuff;
                case (byte)eSpellType.ColdResistBuff:
                    return eEffect.ColdResistBuff;
                case (byte)eSpellType.MatterResistBuff:
                    return eEffect.MatterResistBuff;

                //regen
                case (byte)eSpellType.HealthRegenBuff:
                    return eEffect.HealthRegenBuff;
                case (byte)eSpellType.EnduranceRegenBuff:
                    return eEffect.EnduranceRegenBuff;
                case (byte)eSpellType.PowerRegenBuff:
                    return eEffect.PowerRegenBuff;

                #endregion

                #region Negative Effects

                //persistent negative effects
                case (byte)eSpellType.StyleBleeding:
                    return eEffect.Bleed;
                case (byte)eSpellType.DamageOverTime:
                    return eEffect.DamageOverTime;
                case (byte)eSpellType.Charm:
                    return eEffect.Charm;
                case (byte)eSpellType.SpeedDecrease:
                    return eEffect.MovementSpeedDebuff;
                case (byte)eSpellType.MeleeDamageDebuff:
                    return eEffect.MeleeDamageDebuff;
                case (byte)eSpellType.StyleCombatSpeedDebuff:
                case (byte)eSpellType.CombatSpeedDebuff:
                    return eEffect.MeleeHasteDebuff;
                case (byte)eSpellType.Disease:
                    return eEffect.Disease;

                //Crowd Control Effects
                case (byte)eSpellType.StyleStun:
                case (byte)eSpellType.Stun:
                    return eEffect.Stun;
                //case (byte)eSpellType.StunImmunity: // Not implemented
                //    return eEffect.StunImmunity;
                case (byte)eSpellType.Mesmerize:
                    return eEffect.Mez;
                //case (byte)eSpellType.MezImmunity: // Not implemented
                //    return eEffect.MezImmunity;
                case (byte)eSpellType.StyleSpeedDecrease:
                    return eEffect.MeleeSnare;
                //case (byte)eSpellType.Snare: // May work off of SpeedDecrease
                //    return eEffect.Snare;
                //case (byte)eSpellType.SnareImmunity: // Not implemented
                //    return eEffect.SnareImmunity;
                case (byte)eSpellType.Nearsight:
                    return eEffect.Nearsight;

                //stat debuffs
                case (byte)eSpellType.StrengthDebuff:
                    return eEffect.StrengthDebuff;
                case (byte)eSpellType.DexterityDebuff:
                    return eEffect.DexterityDebuff;
                case (byte)eSpellType.ConstitutionDebuff:
                    return eEffect.ConstitutionDebuff;
                case (byte)eSpellType.StrengthConstitutionDebuff:
                    return eEffect.StrConDebuff;
                case (byte)eSpellType.DexterityQuicknessDebuff:
                    return eEffect.DexQuiDebuff;
                //case (byte)eSpellType.AcuityDebuff: //Not sure what this is yet
                //return eEffect.Acuity;
                case (byte)eSpellType.ArmorAbsorptionDebuff:
                    return eEffect.ArmorAbsorptionDebuff;
                case (byte)eSpellType.ArmorFactorDebuff:
                    return eEffect.ArmorFactorDebuff;

                //resist debuffs
                case (byte)eSpellType.BodyResistDebuff:
                    return eEffect.BodyResistDebuff;
                case (byte)eSpellType.SpiritResistDebuff:
                    return eEffect.SpiritResistDebuff;
                case (byte)eSpellType.EnergyResistDebuff:
                    return eEffect.EnergyResistDebuff;
                case (byte)eSpellType.HeatResistDebuff:
                    return eEffect.HeatResistDebuff;
                case (byte)eSpellType.ColdResistDebuff:
                    return eEffect.ColdResistDebuff;
                case (byte)eSpellType.MatterResistDebuff:
                    return eEffect.MatterResistDebuff;

                //misc
                case (byte)eSpellType.DirectDamage:
                    return eEffect.DirectDamage;

                #endregion

                default:
                    Console.WriteLine($"Unable to map effect for ECSGameEffect! {((eSpellType)spellHandler.Spell.SpellType).ToString()}");
                    return eEffect.Unknown;
            }
        }
    }
}