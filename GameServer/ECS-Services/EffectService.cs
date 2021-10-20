using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.Events;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.PropertyCalc;
using DOL.GS.ServerProperties;
using DOL.GS.SpellEffects;
using DOL.GS.Spells;
using DOL.Language;
using ECS.Debug;

namespace DOL.GS
{
    public static class EffectService
    {
        private const string ServiceName = "EffectService";

        static EffectService()
        {
            //This should technically be the world manager
            EntityManager.AddService(typeof(EffectService));
        }


        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            foreach (var e in EntityManager.GetAllEffects())
            {
                if (e.CancelEffect)
                {
                    HandleCancelEffect(e);
                }
                else if (e.IsDisabled)
                {
                    HandleCancelEffect(e);
                }
                else
                {
                    HandlePropertyModification(e);
                }
                // EntityManager.RemoveEffect() is called inside the Handle functions to ensure the conditions
                // above never result in us removing an effect from the queue without attempting to process it.
            }

            Diagnostics.StopPerfCounter(ServiceName);
        }

        private static void HandlePropertyModification(ECSGameEffect e)
        {
            EntityManager.RemoveEffect(e);

            if (e.Owner == null)
            {
                //Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            EffectListComponent effectList = e.Owner.effectListComponent;
            if (effectList == null)
            {
                //Console.WriteLine($"No effect list found for {e.Owner}");
                return;
            }

            // Early out if we're trying to add an effect that is already present.
            else if (!effectList.AddEffect(e))
            {
                SendSpellResistAnimation(e);
                return;
            }

            // Update the Concentration List if Conc Buff/Song/Chant.
            if (e.ShouldBeAddedToConcentrationList())
            {
                if (e.SpellHandler.Caster != null && e.SpellHandler.Caster.ConcentrationEffects != null)
                {
                    e.SpellHandler.Caster.ConcentrationEffects.Add(e);
                }
            }

            e.OnStartEffect();

            if (e.EffectType == eEffect.Pulse)
            {
                if (!e.RenewEffect && e.SpellHandler.Spell.IsInstantCast)
                    ((SpellHandler)e.SpellHandler).SendCastAnimation();
            }
            else 
            { 
                if (!(e is ECSImmunityEffect))
                {
                    if (!e.RenewEffect)
                        SendSpellAnimation(e);

                    if ((e.FromSpell && e.SpellHandler.Spell.IsConcentration && !e.SpellHandler.Spell.IsPulsing) || (!e.IsBuffActive && !e.IsDisabled))
                    {                       
                        if (e.EffectType == eEffect.EnduranceRegenBuff)
                        {
                            //Console.WriteLine("Applying EnduranceRegenBuff");
                            var handler = e.SpellHandler as EnduranceRegenSpellHandler;
                            ApplyBonus(e.Owner, handler.BonusCategory1, handler.Property1, e.SpellHandler.Spell.Value, e.Effectiveness, false);
                        }                                                                   
                        
                        e.IsBuffActive = true;
                    }                                     
                }
                //else
                //{
                //    if (e.Owner is GamePlayer immunePlayer)
                //    {
                //        immunePlayer.Out.SendUpdateIcons(e.Owner.effectListComponent.Effects.Values.Where(ef => ef.Icon != 0).ToList(), ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                //    }
                //}

                UpdateEffectIcons(e);
            }
        }

        private static void UpdateEffectIcons(ECSGameEffect e)
        {
            if (e.Owner is GamePlayer player)
            {
                List<ECSGameEffect> ecsList = new List<ECSGameEffect>();

                if (e.PreviousPosition >= 0)
                {
                    List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                    ecsList.AddRange(playerEffects.Skip(e.PreviousPosition));
                }
                else
                    ecsList.Add(e);

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                SendPlayerUpdates(player);
            }
            else if (e.Owner is GameNPC)
            {
                IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                if (npc != null)
                    npc.UpdatePetWindow();
            }
        }

        private static void HandleCancelEffect(ECSGameEffect e)
        {
            EntityManager.RemoveEffect(e);

            //Console.WriteLine($"Handling Cancel Effect {e.SpellHandler.ToString()}");

            if (!e.Owner.effectListComponent.RemoveEffect(e))
            {
                //Console.WriteLine("Unable to remove effect!");
                return;
            }

            e.OnStopEffect();
            e.TryApplyImmunity();

            if (!e.IsBuffActive)
            {
                //Console.WriteLine("Buff not active! {0} on {1}", e.SpellHandler.Spell.Name, e.Owner.Name);
            }
            else if (e.EffectType != eEffect.Pulse)
            {
                if (!(e is ECSImmunityEffect) )
                {                  
                    if (e.EffectType == eEffect.EnduranceRegenBuff)
                    {
                        //Console.WriteLine("Removing EnduranceRegenBuff");
                        var handler = e.SpellHandler as EnduranceRegenSpellHandler;
                        ApplyBonus(e.Owner, handler.BonusCategory1, handler.Property1, e.SpellHandler.Spell.Value, e.Effectiveness, true);
                    }                                                           
                }
            }

            e.IsBuffActive = false;
            // Update the Concentration List if Conc Buff/Song/Chant.
            if (e.CancelEffect && e.ShouldBeRemovedFromConcentrationList())
            {
                if (e.SpellHandler.Caster != null && e.SpellHandler.Caster.ConcentrationEffects != null)
                {
                    e.SpellHandler.Caster.ConcentrationEffects.Remove(e);
                }
            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
                //Now update EffectList
                List<ECSGameEffect> ecsList = new List<ECSGameEffect>();
                List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(e)));

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
            }
            else if (e.Owner is GameNPC)
            {
                IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                if (npc != null)
                    npc.UpdatePetWindow();
            }
        }

        /// <summary>
        /// Enqueues an ECSGameEffect to be canceled on the next tick.
        /// </summary>
        public static void RequestCancelEffect(ECSGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((effect.SpellHandler != null && !effect.SpellHandler.HasPositiveEffect) || effect is ECSImmunityEffect))
            {
                GamePlayer player = effect.Owner as GamePlayer;
                if (player != null)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            // playerCanceled param isn't used but it's there in case we eventually want to...
            effect.CancelEffect = true;
            effect.ExpireTick = GameLoop.GameLoopTime - 1;
            EntityManager.AddEffect(effect);
        }

        /// <summary>
        /// Enqueues an ECSGameEffect (as a IConcentrationEffect) to be canceled on the next tick.
        /// </summary>
        public static void RequestCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            ECSGameEffect effect = concEffect as ECSGameEffect;
            if (effect != null)
            {
                RequestCancelEffect(effect, playerCanceled);

                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.LastPulseCast = null;
            }
        }
        /// <summary>
        /// Enques an ECSGameEffect to be disabled/enabled on next tick
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="disable"></param>
        public static void RequestDisableEffect(ECSGameEffect effect, bool disable)
        {
            effect.IsDisabled = disable;
            effect.RenewEffect = !disable;
            EntityManager.AddEffect(effect);
        }

        public static void SendSpellAnimation(ECSGameEffect e)
        {
            if (!e.FromSpell)
                return;
            
            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in e.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
            }
        }

        public static eEffect GetEffectFromSpell(Spell spell)
        {
            //Console.WriteLine("Spell of type: " + (spell.SpellType).ToString());

            switch (spell.SpellType)
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
                case (byte)eSpellType.SpeedOfTheRealm:
                case (byte)eSpellType.SpeedEnhancement:
                    return eEffect.MovementSpeedBuff;
                case (byte)eSpellType.HealOverTime:
                    return eEffect.HealOverTime;
                case (byte)eSpellType.CombatHeal:
                    return eEffect.CombatHeal;

                //stats
                case (byte)eSpellType.StrengthBuff:
                    return eEffect.StrengthBuff;
                case (byte)eSpellType.DexterityBuff:
                    return eEffect.DexterityBuff;
                case (byte)eSpellType.ConstitutionBuff:
                    return eEffect.ConstitutionBuff;
                case (byte)eSpellType.StrengthConstitutionBuff:
                    return eEffect.StrengthConBuff;
                case (byte)eSpellType.DexterityQuicknessBuff:
                    return eEffect.DexQuickBuff;
                case (byte)eSpellType.AcuityBuff:
                    return eEffect.AcuityBuff;
                case (byte)eSpellType.ArmorAbsorptionBuff:
                    return eEffect.ArmorAbsorptionBuff;
                case (byte)eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;
                case (byte)eSpellType.ArmorFactorBuff:
                    if (spell.IsSpec)
                        return eEffect.SpecAFBuff; 
                    else
                        return eEffect.BaseAFBuff;

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
                case (byte)eSpellType.BodySpiritEnergyBuff:
                    return eEffect.BodySpiritEnergyBuff;
                case (byte)eSpellType.HeatColdMatterBuff:
                    return eEffect.HeatColdMatterBuff;


                //regen
                case (byte)eSpellType.HealthRegenBuff:
                    return eEffect.HealthRegenBuff;
                case (byte)eSpellType.EnduranceRegenBuff:
                    return eEffect.EnduranceRegenBuff;
                case (byte)eSpellType.PowerRegenBuff:
                    return eEffect.PowerRegenBuff;

                case (byte)eSpellType.OffensiveProc:
                    return eEffect.OffensiveProc;
                case (byte)eSpellType.DefensiveProc:
                    return eEffect.DefensiveProc;
                case (byte)eSpellType.HereticPiercingMagic:
                    return eEffect.HereticPiercingMagic;
                #endregion

                #region Negative Effects

                //persistent negative effects
                case (byte)eSpellType.StyleBleeding:
                    return eEffect.Bleed;
                case (byte)eSpellType.DamageOverTime:
                    return eEffect.DamageOverTime;
                case (byte)eSpellType.Charm:
                    return eEffect.Charm;
                case (byte)eSpellType.DamageSpeedDecrease:
                case (byte)eSpellType.StyleSpeedDecrease:
                case (byte)eSpellType.SpeedDecrease:
                case (byte)eSpellType.UnbreakableSpeedDecrease:
                    return eEffect.MovementSpeedDebuff;
                case (byte)eSpellType.MeleeDamageDebuff:
                    return eEffect.MeleeDamageDebuff;
                case (byte)eSpellType.StyleCombatSpeedDebuff:
                case (byte)eSpellType.CombatSpeedDebuff:
                    return eEffect.MeleeHasteDebuff;
                case (byte)eSpellType.Disease:
                    return eEffect.Disease;
                case (byte)eSpellType.Confusion:
                    return eEffect.Confusion;

                //Crowd Control Effects
                case (byte)eSpellType.StyleStun:
                case (byte)eSpellType.Stun:
                    return eEffect.Stun;
                //case (byte)eSpellType.StunImmunity: // ImmunityEffect
                //return eEffect.StunImmunity;
                case (byte)eSpellType.Mesmerize:
                    return eEffect.Mez;
                case (byte)eSpellType.MesmerizeDurationBuff:
                    return eEffect.MesmerizeDurationBuff;
                //case (byte)eSpellType.MezImmunity: // ImmunityEffect
                //    return eEffect.MezImmunity;
                //case (byte)eSpellType.StyleSpeedDecrease:
                //    return eEffect.MeleeSnare;
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
                case (byte)eSpellType.WeaponSkillConstitutionDebuff:
                    return eEffect.WsConDebuff;
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
                case (byte)eSpellType.SavageCombatSpeedBuff:
                case (byte)eSpellType.SavageCrushResistanceBuff:
                case (byte)eSpellType.SavageDPSBuff:
                case (byte)eSpellType.SavageEnduranceHeal:
                case (byte)eSpellType.SavageEvadeBuff:
                case (byte)eSpellType.SavageParryBuff:
                case (byte)eSpellType.SavageSlashResistanceBuff:
                case (byte)eSpellType.SavageThrustResistanceBuff:
                    return eEffect.SavageBuff;
                case (byte)eSpellType.DirectDamage:
                    return eEffect.DirectDamage;
                case (byte)eSpellType.FacilitatePainworking:
                    return eEffect.FacilitatePainworking;
                case (byte)eSpellType.FatigueConsumptionBuff:
                    return eEffect.FatigueConsumptionBuff;
                case (byte)eSpellType.DirectDamageWithDebuff:
                    if (spell.DamageType == eDamageType.Body)
                        return eEffect.BodyResistDebuff;
                    else if (spell.DamageType == eDamageType.Cold)
                        return eEffect.ColdResistDebuff;
                    else
                        return eEffect.Unknown;
                case (byte)eSpellType.PiercingMagic:
                    return eEffect.PiercingMagic;
                case (byte)eSpellType.PveResurrectionIllness:
                    return eEffect.ResurrectionIllness;
                case (byte)eSpellType.RvrResurrectionIllness:
                    return eEffect.RvrResurrectionIllness;
                //pets
                case (byte)eSpellType.SummonTheurgistPet:
                case (byte)eSpellType.SummonNoveltyPet:
                case (byte)eSpellType.SummonAnimistPet:
                case (byte)eSpellType.SummonAnimistFnF:
                case (byte)eSpellType.SummonSpiritFighter:
                case (byte)eSpellType.SummonHunterPet:
                case (byte)eSpellType.SummonUnderhill:
                case (byte)eSpellType.SummonDruidPet:
                case (byte)eSpellType.SummonSimulacrum:
                case (byte)eSpellType.SummonNecroPet:
                case (byte)eSpellType.SummonCommander:
                case (byte)eSpellType.SummonMinion:
                    return eEffect.Pet;

                #endregion

                default:
                    //Console.WriteLine($"Unable to map effect for ECSGameEffect! {spell}");
                    return eEffect.Unknown;
            }
        }

        public static void SendSpellResistAnimation(ECSGameEffect e)
        {
            if (!e.FromSpell)
                return;

            GameLiving target = e.SpellHandler.GetTarget() != null ? e.SpellHandler.GetTarget() : e.SpellHandler.Caster;
            //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            foreach (GamePlayer player in target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
            {
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 0);
            }
        }

        private static void SendPlayerUpdates(GamePlayer player)
        {
            if (player == null)
                return;

            player.Out.SendCharStatsUpdate();
            player.Out.SendCharResistsUpdate();
            player.Out.SendUpdateWeaponAndArmorStats();
            player.UpdateEncumberance();
            player.UpdatePlayerStatus();
            player.Out.SendUpdatePlayer();
            
            if (player.Group != null)
            {
                player.Group.UpdateMember(player, true, false);
            }
        }

        public static List<eProperty> GetPropertiesFromEffect(eEffect e)
        {
            List<eProperty> list = new List<eProperty>();
            switch (e)
            {
                //stats
                case eEffect.StrengthBuff:
                case eEffect.StrengthDebuff:
                    list.Add(eProperty.Strength);
                    return list;
                case eEffect.DexterityBuff:
                case eEffect.DexterityDebuff:
                    list.Add(eProperty.Dexterity);
                    return list;
                case eEffect.ConstitutionBuff:
                case eEffect.ConstitutionDebuff:
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.AcuityBuff:
                case eEffect.AcuityDebuff:
                    list.Add(eProperty.Acuity);
                    return list;

                case eEffect.StrengthConBuff:
                case eEffect.StrConDebuff:
                    list.Add(eProperty.Strength);
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.WsConDebuff:
                    list.Add(eProperty.WeaponSkill);
                    list.Add(eProperty.Constitution);
                    return list;
                case eEffect.DexQuickBuff:
                case eEffect.DexQuiDebuff:
                    list.Add(eProperty.Dexterity);
                    list.Add(eProperty.Quickness);
                    return list;
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorFactorDebuff:
                    list.Add(eProperty.ArmorFactor);
                    return list;
                case eEffect.ArmorAbsorptionBuff:
                case eEffect.ArmorAbsorptionDebuff:
                    list.Add(eProperty.ArmorAbsorption);
                    return list;
                case eEffect.MeleeDamageBuff:
                case eEffect.MeleeDamageDebuff:
                    list.Add(eProperty.MeleeDamage);
                    return list;
                //resists
                case eEffect.NaturalResistDebuff:
                    list.Add(eProperty.Resist_Natural);
                    return list;
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
                case eEffect.HeatColdMatterBuff:
                    list.Add(eProperty.Resist_Heat);
                    list.Add(eProperty.Resist_Cold);
                    list.Add(eProperty.Resist_Matter);
                    return list;
                case eEffect.BodySpiritEnergyBuff:
                    list.Add(eProperty.Resist_Body);
                    list.Add(eProperty.Resist_Spirit);
                    list.Add(eProperty.Resist_Energy);
                    return list;
                case eEffect.AllMagicResistsBuff:
                    list.Add(eProperty.Resist_Body);
                    list.Add(eProperty.Resist_Spirit);
                    list.Add(eProperty.Resist_Energy);
                    list.Add(eProperty.Resist_Heat);
                    list.Add(eProperty.Resist_Cold);
                    list.Add(eProperty.Resist_Matter);
                    return list;

                case eEffect.SlashResistBuff:
                case eEffect.SlashResistDebuff:
                    list.Add(eProperty.Resist_Slash);
                    return list;
                case eEffect.ThrustResistBuff:
                case eEffect.ThrustResistDebuff:
                    list.Add(eProperty.Resist_Thrust);
                    return list;
                case eEffect.CrushResistBuff:
                case eEffect.CrushResistDebuff:
                    list.Add(eProperty.Resist_Crush);
                    return list;
                case eEffect.AllMeleeResistsBuff:
                case eEffect.AllMeleeResistsDebuff:
                    list.Add(eProperty.Resist_Crush);
                    list.Add(eProperty.Resist_Thrust);
                    list.Add(eProperty.Resist_Slash);
                    return list;

                case eEffect.HealthRegenBuff:
                    list.Add(eProperty.HealthRegenerationRate);
                    return list;
                case eEffect.PowerRegenBuff:
                    list.Add(eProperty.PowerRegenerationRate);
                    return list;
                case eEffect.EnduranceRegenBuff:
                    list.Add(eProperty.EnduranceRegenerationRate);
                    return list;
                case eEffect.MeleeHasteBuff:
                case eEffect.MeleeHasteDebuff:
                    list.Add(eProperty.MeleeSpeed);
                    return list;
                case eEffect.MovementSpeedBuff:
                case eEffect.MovementSpeedDebuff:
                    list.Add(eProperty.MaxSpeed);
                    return list;
                case eEffect.MesmerizeDurationBuff:
                    list.Add(eProperty.MesmerizeDurationReduction);
                    return list;
                case eEffect.FatigueConsumptionBuff:
                    list.Add(eProperty.FatigueConsumption);
                    return list;
                default:
                    //Console.WriteLine($"Unable to find property mapping for: {e}");
                    return list;
            }
        }

        private static Boolean isDebuff(eEffect e)
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
                case eEffect.StrengthBuff:
                case eEffect.DexterityBuff:
                case eEffect.ConstitutionBuff:
                case eEffect.AcuityBuff:
                case eEffect.StrengthConBuff:
                case eEffect.DexQuickBuff:
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
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
                    return false;

                case eEffect.StrengthDebuff:
                case eEffect.DexterityDebuff:
                case eEffect.ConstitutionDebuff:
                case eEffect.AcuityDebuff:
                case eEffect.StrConDebuff:
                case eEffect.WsConDebuff:
                case eEffect.DexQuiDebuff:
                case eEffect.ArmorFactorDebuff:
                case eEffect.ArmorAbsorptionDebuff:
                case eEffect.BodyResistDebuff:
                case eEffect.SpiritResistDebuff:
                case eEffect.EnergyResistDebuff:
                case eEffect.HeatResistDebuff:
                case eEffect.ColdResistDebuff:
                case eEffect.MatterResistDebuff:
                case eEffect.MeleeHasteDebuff:
                case eEffect.MovementSpeedDebuff:
                case eEffect.Disease:
                case eEffect.Nearsight:
                case eEffect.MeleeDamageDebuff:
                    return true;
                default:
                    //Console.WriteLine($"Unable to detect debuff status for {e}");
                    return false;
            }


        }

        

        /// <summary>
		/// Method used to apply bonuses
		/// </summary>
		/// <param name="owner"></param>
		/// <param name="BonusCat"></param>
		/// <param name="Property"></param>
		/// <param name="Value"></param>
		/// <param name="IsSubstracted"></param>
		private static void ApplyBonus(GameLiving owner, eBuffBonusCategory BonusCat, eProperty Property, double Value, double Effectiveness, bool IsSubstracted)
        {
            int effectiveValue = (int)(Value * Effectiveness);

            IPropertyIndexer tblBonusCat;
            if (Property != eProperty.Undefined)
            {
                tblBonusCat = GetBonusCategory(owner, BonusCat);
                //Console.WriteLine($"Value before: {tblBonusCat[(int)Property]}");
                if (IsSubstracted)
                    tblBonusCat[(int)Property] -= effectiveValue;
                else
                    tblBonusCat[(int)Property] += effectiveValue;
                //Console.WriteLine($"Value after: {tblBonusCat[(int)Property]}");
            }
        } 

        private static IPropertyIndexer GetBonusCategory(GameLiving target, eBuffBonusCategory categoryid)
        {
            IPropertyIndexer bonuscat = null;
            switch (categoryid)
            {
                case eBuffBonusCategory.BaseBuff:
                    bonuscat = target.BaseBuffBonusCategory;
                    break;
                case eBuffBonusCategory.SpecBuff:
                    bonuscat = target.SpecBuffBonusCategory;
                    break;
                case eBuffBonusCategory.Debuff:
                    bonuscat = target.DebuffCategory;
                    break;
                case eBuffBonusCategory.Other:
                    bonuscat = target.BuffBonusCategory4;
                    break;
                case eBuffBonusCategory.SpecDebuff:
                    bonuscat = target.SpecDebuffCategory;
                    break;
                case eBuffBonusCategory.AbilityBuff:
                    bonuscat = target.AbilityBonus;
                    break;
                default:
                    //if (log.IsErrorEnabled)
                    //Console.WriteLine("BonusCategory not found " + categoryid + "!");
                    break;
            }
            return bonuscat;
        }
    }
}