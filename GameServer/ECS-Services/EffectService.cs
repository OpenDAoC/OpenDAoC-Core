using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
        static int _segmentsize = 100;
        static List<Task> _tasks = new List<Task>();

        private const string ServiceName = "EffectService";

        static EffectService()
        {
            //This should technically be the world manager
            EntityManager.AddService(typeof(EffectService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            ECSGameEffect[] arr = EntityManager.GetAllEffects();

            Parallel.ForEach(arr, effect =>
            {
                if (effect == null)
                    return;

                if (effect.CancelEffect || effect.IsDisabled)
                {
                    HandleCancelEffect(effect);
                }
                else
                {
                    HandlePropertyModification(effect);
                }
            });
            
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
                if (e is ECSGameSpellEffect spell && !spell.SpellHandler.Spell.IsPulsing)
                    SendSpellResistAnimation(e as ECSGameSpellEffect);
                return;
            }

            ECSGameSpellEffect spellEffect = e as ECSGameSpellEffect;

            // Update the Concentration List if Conc Buff/Song/Chant.
            if (spellEffect != null && spellEffect.ShouldBeAddedToConcentrationList() && !spellEffect.RenewEffect)
            {
                if (spellEffect.SpellHandler.Caster != null && spellEffect.SpellHandler.Caster.ConcentrationEffects != null)
                {
                    spellEffect.SpellHandler.Caster.UsedConcentration += spellEffect.SpellHandler.Spell.Concentration;
                    spellEffect.SpellHandler.Caster.ConcentrationEffects.Add(spellEffect);

                    if (spellEffect.SpellHandler.Caster is GamePlayer p)
                        p.Out.SendConcentrationList();
                }
            }

            if (spellEffect != null)
            {
                if (e.EffectType == eEffect.Pulse)
                {
                    if (!e.RenewEffect && spellEffect.SpellHandler.Spell.IsInstantCast)
                        ((SpellHandler)spellEffect.SpellHandler).SendCastAnimation();
                }
                else
                {
                    if (!spellEffect.RenewEffect && !(spellEffect is ECSImmunityEffect))
                        SendSpellAnimation((ECSGameSpellEffect)e);

                    if (e is StatDebuffECSEffect && spellEffect.SpellHandler.Spell.CastTime == 0)
                        StatDebuffECSEffect.TryDebuffInterrupt(spellEffect.SpellHandler.Spell, e.OwnerPlayer, spellEffect.SpellHandler.Caster);

                    if ((spellEffect.SpellHandler.Spell.IsConcentration && !spellEffect.SpellHandler.Spell.IsPulsing) || (!spellEffect.IsBuffActive && !spellEffect.IsDisabled)
                        || spellEffect is SavageBuffECSGameEffect)
                    {
                        //if (spellEffect.EffectType == eEffect.EnduranceRegenBuff)
                        //{
                        //    //Console.WriteLine("Applying EnduranceRegenBuff");
                        //    var handler = spellEffect.SpellHandler as EnduranceRegenSpellHandler;
                        //    ApplyBonus(spellEffect.Owner, handler.BonusCategory1, handler.Property1, spellEffect.SpellHandler.Spell.Value, 1, false);
                        //}
                        e.OnStartEffect();
                        e.IsBuffActive = true;
                    }
                }
            }
            else
                e.OnStartEffect();

            UpdateEffectIcons(e);
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
                {
                    if (e is ECSGameSpellEffect spellEffect && AllStatsBarrel.BuffList.Contains(spellEffect.SpellHandler.Spell.ID))
                    {
                        List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(playerEffects.Count - AllStatsBarrel.BuffList.Count));
                    }
                    else
                        ecsList.Add(e);
                }

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                SendPlayerUpdates(player);
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNPC)
            {
                IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                if (npc != null)
                    npc.UpdatePetWindow();
                if (npc?.Body is NecromancerPet)
                    SendPlayerUpdates(npc.Owner as GamePlayer);
            }
        }

        private static void HandleCancelEffect(ECSGameEffect e)
        {
            //Console.WriteLine($"Handling Cancel Effect {e.SpellHandler.ToString()}");
            EntityManager.RemoveEffect(e);
            if (!e.Owner.effectListComponent.RemoveEffect(e))
            {
                //Console.WriteLine("Unable to remove effect!");
                return;
            }

            ECSGameSpellEffect spellEffect = e as ECSGameSpellEffect;
            if (spellEffect != null)
            {
                if (!spellEffect.IsBuffActive)
                {
                    //Console.WriteLine("Buff not active! {0} on {1}", e.SpellHandler.Spell.Name, e.Owner.Name);
                }
                else if (spellEffect.EffectType != eEffect.Pulse)
                {
                    if (!(spellEffect is ECSImmunityEffect))
                    {
                        //if (spellEffect.EffectType == eEffect.EnduranceRegenBuff)
                        //{
                        //    //Console.WriteLine("Removing EnduranceRegenBuff");
                        //    var handler = spellEffect.SpellHandler as EnduranceRegenSpellHandler;
                        //    ApplyBonus(spellEffect.Owner, handler.BonusCategory1, handler.Property1, spellEffect.SpellHandler.Spell.Value, spellEffect.Effectiveness, true);
                        //}
                        e.OnStopEffect();
                    }
                }

                e.IsBuffActive = false;
                // Update the Concentration List if Conc Buff/Song/Chant.
                if (e.CancelEffect && e.ShouldBeRemovedFromConcentrationList())
                {
                    if (spellEffect.SpellHandler.Caster != null && spellEffect.SpellHandler.Caster.ConcentrationEffects != null)
                    {
                        spellEffect.SpellHandler.Caster.UsedConcentration -= spellEffect.SpellHandler.Spell.Concentration;
                        spellEffect.SpellHandler.Caster.ConcentrationEffects.Remove(spellEffect);

                        if (spellEffect.SpellHandler.Caster is GamePlayer p)
                            p.Out.SendConcentrationList();
                    }
                }
            }
            else
                e.OnStopEffect();

            e.TryApplyImmunity();

            if (!e.IsDisabled && e.Owner.effectListComponent.Effects.ContainsKey(e.EffectType))
            {
                var enableEffect = e.Owner.effectListComponent.GetSpellEffects(e.EffectType).OrderByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
                if (enableEffect != null && enableEffect.IsDisabled)
                    RequestEnableEffect(enableEffect);
            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);
                //Now update EffectList
                List<ECSGameEffect> ecsList = new List<ECSGameEffect>();
                List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(e)));

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent._lastUpdateEffectsCount);
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNPC)
            {
                IControlledBrain npc = ((GameNPC)e.Owner).Brain as IControlledBrain;
                if (npc != null)
                    npc.UpdatePetWindow();
            }
        }

        /// <summary>
        /// Immediately cancels an ECSGameEffect.
        /// </summary>
        public static void RequestCancelEffect(ECSGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            if (effect is QuickCastECSGameEffect quickCast)
            {
                quickCast.Cancel(true);
                return;
            }

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((!effect.HasPositiveEffect) || effect is ECSImmunityEffect))
            {
                GamePlayer player = effect.Owner as GamePlayer;
                if (player != null)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            effect.CancelEffect = true;
            effect.ExpireTick = GameLoop.GameLoopTime - 1;
            EntityManager.AddEffect(effect);
        }

        /// <summary>
        /// Immediately cancels an ECSGameSpellEffect (as a IConcentrationEffect).
        /// </summary>
        public static void RequestCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            ECSGameSpellEffect effect = concEffect as ECSGameSpellEffect;
            if (effect != null)
            {
                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.LastPulseCast = null;

                RequestCancelEffect(effect, playerCanceled);
            }
        }


        /// <summary>
        /// Immediately removes an ECSGameEffect.
        /// </summary>
        public static void RequestImmediateCancelEffect(ECSGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((!effect.HasPositiveEffect) || effect is ECSImmunityEffect))
            {
                GamePlayer player = effect.Owner as GamePlayer;
                if (player != null)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            // playerCanceled param isn't used but it's there in case we eventually want to...
            effect.CancelEffect = true;
            effect.ExpireTick = GameLoop.GameLoopTime - 1;
            HandleCancelEffect(effect);
        }


        /// <summary>
        /// Immediately removes an ECSGameEffect (as a IConcentrationEffect).
        /// </summary>
        public static void RequestImmediateCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            ECSGameSpellEffect effect = concEffect as ECSGameSpellEffect;
            if (effect != null)
            {
                RequestImmediateCancelEffect(effect, playerCanceled);

                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.LastPulseCast = null;
            }
        }

        /// <summary>
        /// Immediately starts an ECSGameEffect.
        /// </summary>
        /// <param name="effect"></param>
        public static void RequestStartEffect(ECSGameEffect effect)
        {
            HandlePropertyModification(effect);
        }

        /// <summary>
        /// Immediately disables an ECSGameEffect.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="disable"></param>
        public static void RequestDisableEffect(ECSGameEffect effect)
        {
            effect.IsDisabled = true;
            effect.RenewEffect = false;
            HandleCancelEffect(effect);
        }

        /// <summary>
        /// Immediately enables a previously disabled ECSGameEffect.
        /// </summary>
        /// <param name="effect"></param>
        /// <param name="disable"></param>
        public static void RequestEnableEffect(ECSGameEffect effect)
        {
            if (!effect.IsDisabled)
                return;
            
            effect.IsDisabled = false;
            effect.RenewEffect = true;
            HandlePropertyModification(effect);
        }

        public static void SendSpellAnimation(ECSGameSpellEffect e)
        {
            if (e != null)
            {
                if (e.SpellHandler.Spell.IsPulsing && e.Owner != e.SpellHandler.Caster && e.SpellHandler.HasPositiveEffect && 
                    e.SpellHandler.Spell.SpellType != (byte)eSpellType.DamageShield)
                    return;

                //foreach (GamePlayer player in e.SpellHandler.Target.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                foreach (GamePlayer player in e.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                        player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 1);
                }
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
                case (byte)eSpellType.SlashResistDebuff:
                    return eEffect.SlashResistDebuff;

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
                    else if (spell.DamageType == eDamageType.Heat)
                        return eEffect.HeatResistDebuff;
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

        public static void SendSpellResistAnimation(ECSGameSpellEffect e)
        {
            if (e is null)
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