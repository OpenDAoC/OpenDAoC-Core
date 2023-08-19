using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class EffectService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(EffectService);

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<ECSGameEffect> list = EntityManager.UpdateAndGetAll<ECSGameEffect>(EntityManager.EntityType.Effect, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                ECSGameEffect effect = list[i];

                try
                {
                    if (effect?.EntityManagerId.IsSet != true)
                        return;

                    long startTick = GameLoop.GetCurrentTime();

                    if (effect.CancelEffect || effect.IsDisabled)
                        HandleCancelEffect(effect);
                    else
                        HandlePropertyModification(effect);

                    EntityManager.Remove(effect);

                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for Effect: {effect}  Owner: {effect.OwnerName} Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, effect, effect.Owner);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void HandlePropertyModification(ECSGameEffect e)
        {
            if (e.Owner == null)
            {
                //Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            ECSGameSpellEffect spellEffect = e as ECSGameSpellEffect;
            EffectListComponent effectList = e.Owner.effectListComponent;

            if (effectList == null)
            {
                //Console.WriteLine($"No effect list found for {e.Owner}");
                return;
            }
            // Early out if we're trying to add an effect that is already present.
            else if (!effectList.AddEffect(e))
            {
                if (spellEffect != null && !spellEffect.SpellHandler.Spell.IsPulsing)
                {
                    SendSpellResistAnimation(e as ECSGameSpellEffect);
                    if (spellEffect.SpellHandler.Caster is GameSummonedPet petCaster && petCaster.Owner is GamePlayer casterOwner)
                        ChatUtil.SendResistMessage(casterOwner, "GamePlayer.Caster.Buff.EffectAlreadyActive", e.Owner.GetName(0, true));
                    if (spellEffect.SpellHandler.Caster is GamePlayer playerCaster)
                        ChatUtil.SendResistMessage(playerCaster, "GamePlayer.Caster.Buff.EffectAlreadyActive", e.Owner.GetName(0, true));
                }

                return;
            }

            ISpellHandler spellHandler = spellEffect?.SpellHandler;
            Spell spell = spellHandler?.Spell;
            GameLiving caster = spellHandler?.Caster;

            // Update the Concentration List if Conc Buff/Song/Chant.
            if (spellEffect != null && spellEffect.ShouldBeAddedToConcentrationList() && !spellEffect.RenewEffect)
            {
                if (caster != null && caster.effectListComponent.ConcentrationEffects != null)
                {
                    caster.UsedConcentration += spell.Concentration;

                    lock (caster.effectListComponent.ConcentrationEffectsLock)
                    {
                        caster.effectListComponent.ConcentrationEffects.Add(spellEffect);
                    }

                    if (caster is GamePlayer p)
                        p.Out.SendConcentrationList();
                }
            }

            if (spellEffect != null)
            {
                if (spell.IsPulsing)
                {
                    // This should allow the caster to see the effect of the first tick of a beneficial pulse effect, even when recasted before the existing effect expired.
                    // It means they can spam some spells, but I consider it a good feedback for the player (example: Paladin's endurance chant).
                    // It should also allow harmful effects to be played on the targets, but not the caster (example: Reaver's PBAEs -- the flames, not the waves).
                    // It should prevent double animations too (only checking 'IsHarmful' and 'RenewEffect' would make resist chants play twice).
                    if (spellEffect is ECSPulseEffect)
                    {
                        if (!spell.IsHarmful && spell.SpellType != eSpellType.Charm && !spellEffect.RenewEffect)
                            SendSpellAnimation(spellEffect);
                    }
                    else if (spell.IsHarmful)
                        SendSpellAnimation(spellEffect);
                }
                else if (spellEffect is not ECSImmunityEffect)
                    SendSpellAnimation(spellEffect);

                if (e is StatDebuffECSEffect && spell.CastTime == 0)
                    StatDebuffECSEffect.TryDebuffInterrupt(spell, e.OwnerPlayer, caster);

                if ((!spellEffect.IsBuffActive && !spellEffect.IsDisabled)
                    || spellEffect is SavageBuffECSGameEffect)
                {
                    //if (spellEffect.EffectType == eEffect.EnduranceRegenBuff)
                    //{
                    //    //Console.WriteLine("Applying EnduranceRegenBuff");
                    //    var handler = spellHandler as EnduranceRegenSpellHandler;
                    //    ApplyBonus(spellEffect.Owner, handler.BonusCategory1, handler.Property1, spell.Value, 1, false);
                    //}
                    e.OnStartEffect();
                    e.IsBuffActive = true;
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
                List<ECSGameEffect> ecsList = new();

                if (e.PreviousPosition >= 0)
                {
                    List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                    ecsList.AddRange(playerEffects.Skip(e.PreviousPosition));
                }
                else
                {
                    //fix for Buff Pot Barrel not showing all icons when used
                    if (e is ECSGameSpellEffect spellEffect && AllStatsBarrel.BuffList.Contains(spellEffect.SpellHandler.Spell.ID))
                    {
                        List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(playerEffects.Count - AllStatsBarrel.BuffList.Count));
                    }
                    //fix for Regen Pot not showing all icons when used
                    else if (e is ECSGameSpellEffect regenEffect && AllRegenBuff.RegenList.Contains(regenEffect.SpellHandler.Spell.ID))
                    {
                        List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(playerEffects.Count - AllRegenBuff.RegenList.Count));
                    }
                    else
                        ecsList.Add(e);
                }

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent.GetLastUpdateEffectsCount());
                SendPlayerUpdates(player);
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNPC npc)
            {
                if (npc.Brain is IControlledBrain npcBrain)
                {
                    npcBrain.UpdatePetWindow();

                    if (npc is NecromancerPet)
                        SendPlayerUpdates(npcBrain.Owner as GamePlayer);
                }
            }
        }

        private static void HandleCancelEffect(ECSGameEffect e)
        {
            if (!e.Owner.effectListComponent.RemoveEffect(e))
                return;

            if (e is ECSGameSpellEffect spellEffect)
            {
                if (spellEffect.IsBuffActive && spellEffect.EffectType != eEffect.Pulse && spellEffect is not ECSImmunityEffect)
                    e.OnStopEffect();

                e.IsBuffActive = false;

                // Update the Concentration List if Conc Buff/Song/Chant.
                if (e.CancelEffect && e.ShouldBeRemovedFromConcentrationList())
                {
                    if (spellEffect.SpellHandler.Caster != null && spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects != null)
                    {
                        spellEffect.SpellHandler.Caster.UsedConcentration -= spellEffect.SpellHandler.Spell.Concentration;

                        lock (spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffectsLock)
                        {
                            if (spellEffect is ECSPulseEffect)
                            {
                                for (int i = 0; i < spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects.Count; i++)
                                {
                                    if (spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects[i] is ECSPulseEffect)
                                        spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects.RemoveAt(i);
                                }
                            }
                            else
                                spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects.Remove(spellEffect);
                        }

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
                ECSGameSpellEffect enableEffect = e.Owner.effectListComponent.GetSpellEffects(e.EffectType).OrderByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
                if (enableEffect != null && enableEffect.IsDisabled)
                    RequestEnableEffect(enableEffect);
            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);

                List<ECSGameEffect> ecsList = new();
                List<ECSGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(e)));

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent.GetLastUpdateEffectsCount());
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNPC npc && npc.Brain is IControlledBrain npcBrain)
                npcBrain.UpdatePetWindow();
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
                if (effect.Owner is GamePlayer player)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                return;
            }

            effect.CancelEffect = true;
            effect.ExpireTick = GameLoop.GameLoopTime - 1;
            EntityManager.Add(effect);
        }

        /// <summary>
        /// Immediately cancels an ECSGameSpellEffect (as a IConcentrationEffect).
        /// </summary>
        public static void RequestCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            if (concEffect is ECSGameSpellEffect effect)
            {
                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.ActivePulseSpells.TryRemove(effect.SpellHandler.Spell.SpellType, out Spell _);

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
                if (effect.Owner is GamePlayer player)
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
            if (concEffect is ECSGameSpellEffect effect)
            {
                RequestImmediateCancelEffect(effect, playerCanceled);

                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.ActivePulseSpells.TryRemove(effect.SpellHandler.Spell.SpellType, out Spell _);
            }
        }

        /// <summary>
        /// Immediately starts an ECSGameEffect.
        /// </summary>
        public static void RequestStartEffect(ECSGameEffect effect)
        {
            HandlePropertyModification(effect);
        }

        /// <summary>
        /// Immediately disables an ECSGameEffect.
        /// </summary>
        public static void RequestDisableEffect(ECSGameEffect effect)
        {
            effect.IsDisabled = true;
            effect.RenewEffect = false;
            HandleCancelEffect(effect);
        }

        /// <summary>
        /// Immediately enables a previously disabled ECSGameEffect.
        /// </summary>
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
                ISpellHandler spellHandler = e.SpellHandler;
                Spell spell = spellHandler.Spell;
                GameLiving target;

                // Focus damage shield. Need to figure out why this is needed.
                if (spell.IsPulsing && spell.SpellType == eSpellType.DamageShield)
                    target = spellHandler.Target;
                else
                    target = e.Owner;

                foreach (GamePlayer player in e.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(spellHandler.Caster, target, spell.ClientEffect, 0, false, 1);
            }
        }

        public static eEffect GetEffectFromSpell(Spell spell, bool isBaseLine = true)
        {
            switch (spell.SpellType)
            {
                #region Positive Effects

                case eSpellType.Bladeturn:
                    return eEffect.Bladeturn;
                case eSpellType.DamageAdd:
                    return eEffect.DamageAdd;
                //case eSpellType.DamageReturn:
                //    return eEffect.DamageReturn;
                case eSpellType.DamageShield: // FocusShield: Could be the wrong SpellType here.
                    return eEffect.FocusShield;
                case eSpellType.AblativeArmor:
                    return eEffect.AblativeArmor;
                case eSpellType.MeleeDamageBuff:
                    return eEffect.MeleeDamageBuff;
                case eSpellType.CombatSpeedBuff:
                    return eEffect.MeleeHasteBuff;
                //case eSpellType.Celerity: // Possibly the same as CombatSpeedBuff?
                //    return eEffect.Celerity;
                case eSpellType.SpeedOfTheRealm:
                case eSpellType.SpeedEnhancement:
                    return eEffect.MovementSpeedBuff;
                case eSpellType.HealOverTime:
                    return eEffect.HealOverTime;
                case eSpellType.CombatHeal:
                    return eEffect.CombatHeal;

                // Stats.
                case eSpellType.StrengthBuff:
                    return eEffect.StrengthBuff;
                case eSpellType.DexterityBuff:
                    return eEffect.DexterityBuff;
                case eSpellType.ConstitutionBuff:
                    return eEffect.ConstitutionBuff;
                case eSpellType.StrengthConstitutionBuff:
                    return eEffect.StrengthConBuff;
                case eSpellType.DexterityQuicknessBuff:
                    return eEffect.DexQuickBuff;
                case eSpellType.AcuityBuff:
                    return eEffect.AcuityBuff;
                case eSpellType.ArmorAbsorptionBuff:
                    return eEffect.ArmorAbsorptionBuff;
                case eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;
                case eSpellType.ArmorFactorBuff:
                    return isBaseLine ? eEffect.BaseAFBuff : eEffect.SpecAFBuff;

                // Resists.
                case eSpellType.BodyResistBuff:
                    return eEffect.BodyResistBuff;
                case eSpellType.SpiritResistBuff:
                    return eEffect.SpiritResistBuff;
                case eSpellType.EnergyResistBuff:
                    return eEffect.EnergyResistBuff;
                case eSpellType.HeatResistBuff:
                    return eEffect.HeatResistBuff;
                case eSpellType.ColdResistBuff:
                    return eEffect.ColdResistBuff;
                case eSpellType.MatterResistBuff:
                    return eEffect.MatterResistBuff;
                case eSpellType.BodySpiritEnergyBuff:
                    return eEffect.BodySpiritEnergyBuff;
                case eSpellType.HeatColdMatterBuff:
                    return eEffect.HeatColdMatterBuff;
                case eSpellType.AllMagicResistBuff:
                    return eEffect.AllMagicResistsBuff;

                // Regens.
                case eSpellType.HealthRegenBuff:
                    return eEffect.HealthRegenBuff;
                case eSpellType.EnduranceRegenBuff:
                    return eEffect.EnduranceRegenBuff;
                case eSpellType.PowerRegenBuff:
                    return eEffect.PowerRegenBuff;

                // Misc.
                case eSpellType.OffensiveProc:
                    return eEffect.OffensiveProc;
                case eSpellType.DefensiveProc:
                    return eEffect.DefensiveProc;
                case eSpellType.HereticPiercingMagic:
                    return eEffect.HereticPiercingMagic;

                #endregion
                #region NEGATIVE_EFFECTS

                case eSpellType.StyleBleeding:
                    return eEffect.Bleed;
                case eSpellType.DamageOverTime:
                    return eEffect.DamageOverTime;
                case eSpellType.Charm:
                    return eEffect.Charm;
                case eSpellType.DamageSpeedDecrease:
                case eSpellType.DamageSpeedDecreaseNoVariance:
                case eSpellType.StyleSpeedDecrease:
                case eSpellType.SpeedDecrease:
                case eSpellType.UnbreakableSpeedDecrease:
                    return eEffect.MovementSpeedDebuff;
                case eSpellType.MeleeDamageDebuff:
                    return eEffect.MeleeDamageDebuff;
                case eSpellType.StyleCombatSpeedDebuff:
                case eSpellType.CombatSpeedDebuff:
                    return eEffect.MeleeHasteDebuff;
                case eSpellType.Disease:
                    return eEffect.Disease;
                case eSpellType.Confusion:
                    return eEffect.Confusion;

                // Crowd control.
                case eSpellType.StyleStun:
                case eSpellType.Stun:
                    return eEffect.Stun;
                //case eSpellType.StunImmunity:
                //    return eEffect.StunImmunity;
                case eSpellType.Mesmerize:
                    return eEffect.Mez;
                case eSpellType.MesmerizeDurationBuff:
                    return eEffect.MesmerizeDurationBuff;
                //case eSpellType.MezImmunity:
                //    return eEffect.MezImmunity;
                //case eSpellType.StyleSpeedDecrease:
                //    return eEffect.MeleeSnare;
                //case eSpellType.Snare: // May work off of SpeedDecrease.
                //    return eEffect.Snare;
                //case eSpellType.SnareImmunity: // Not implemented.
                //    return eEffect.SnareImmunity;
                case eSpellType.Nearsight:
                    return eEffect.Nearsight;

                // Stats.
                case eSpellType.StrengthDebuff:
                    return eEffect.StrengthDebuff;
                case eSpellType.DexterityDebuff:
                    return eEffect.DexterityDebuff;
                case eSpellType.ConstitutionDebuff:
                    return eEffect.ConstitutionDebuff;
                case eSpellType.StrengthConstitutionDebuff:
                    return eEffect.StrConDebuff;
                case eSpellType.DexterityQuicknessDebuff:
                    return eEffect.DexQuiDebuff;
                case eSpellType.WeaponSkillConstitutionDebuff:
                    return eEffect.WsConDebuff;
                //case eSpellType.AcuityDebuff: // Not sure what this is yet.
                //    return eEffect.Acuity;
                case eSpellType.ArmorAbsorptionDebuff:
                    return eEffect.ArmorAbsorptionDebuff;
                case eSpellType.ArmorFactorDebuff:
                    return eEffect.ArmorFactorDebuff;

                // Resists.
                case eSpellType.BodyResistDebuff:
                    return eEffect.BodyResistDebuff;
                case eSpellType.SpiritResistDebuff:
                    return eEffect.SpiritResistDebuff;
                case eSpellType.EnergyResistDebuff:
                    return eEffect.EnergyResistDebuff;
                case eSpellType.HeatResistDebuff:
                    return eEffect.HeatResistDebuff;
                case eSpellType.ColdResistDebuff:
                    return eEffect.ColdResistDebuff;
                case eSpellType.MatterResistDebuff:
                    return eEffect.MatterResistDebuff;
                case eSpellType.SlashResistDebuff:
                    return eEffect.SlashResistDebuff;

                // Misc.
                case eSpellType.SavageCombatSpeedBuff:
                    return eEffect.MeleeHasteBuff;
                case eSpellType.SavageCrushResistanceBuff:
                case eSpellType.SavageDPSBuff:
                case eSpellType.SavageEnduranceHeal:
                case eSpellType.SavageEvadeBuff:
                case eSpellType.SavageParryBuff:
                case eSpellType.SavageSlashResistanceBuff:
                case eSpellType.SavageThrustResistanceBuff:
                    return eEffect.SavageBuff;
                case eSpellType.DirectDamage:
                    return eEffect.DirectDamage;
                case eSpellType.FacilitatePainworking:
                    return eEffect.FacilitatePainworking;
                case eSpellType.FatigueConsumptionBuff:
                    return eEffect.FatigueConsumptionBuff;
                case eSpellType.FatigueConsumptionDebuff:
                    return eEffect.FatigueConsumptionDebuff;
                case eSpellType.DirectDamageWithDebuff:
                    if (spell.DamageType == eDamageType.Body)
                        return eEffect.BodyResistDebuff;
                    else if (spell.DamageType == eDamageType.Cold)
                        return eEffect.ColdResistDebuff;
                    else if (spell.DamageType == eDamageType.Heat)
                        return eEffect.HeatResistDebuff;
                    else
                        return eEffect.Unknown;
                case eSpellType.PiercingMagic:
                    return eEffect.PiercingMagic;
                case eSpellType.PveResurrectionIllness:
                    return eEffect.ResurrectionIllness;
                case eSpellType.RvrResurrectionIllness:
                    return eEffect.RvrResurrectionIllness;

                #endregion

                // Pets.
                case eSpellType.SummonTheurgistPet:
                case eSpellType.SummonNoveltyPet:
                case eSpellType.SummonAnimistPet:
                case eSpellType.SummonAnimistFnF:
                case eSpellType.SummonSpiritFighter:
                case eSpellType.SummonHunterPet:
                case eSpellType.SummonUnderhill:
                case eSpellType.SummonDruidPet:
                case eSpellType.SummonSimulacrum:
                case eSpellType.SummonNecroPet:
                case eSpellType.SummonCommander:
                case eSpellType.SummonMinion:
                    return eEffect.Pet;

                default:
                    //Console.WriteLine($"Unable to map effect for ECSGameEffect! {spell}");
                    return eEffect.Unknown;
            }
        }

        public static void SendSpellResistAnimation(ECSGameSpellEffect e)
        {
            if (e is null)
                return;

            foreach (GamePlayer player in e.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(e.SpellHandler.Caster, e.Owner, e.SpellHandler.Spell.ClientEffect, 0, false, 0);
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
            player.Group?.UpdateMember(player, true, false);
        }

        public static List<eProperty> GetPropertiesFromEffect(eEffect e)
        {
            List<eProperty> list = new();

            switch (e)
            {
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
                case eEffect.FatigueConsumptionDebuff:
                    list.Add(eProperty.FatigueConsumption);
                    return list;

                default:
                    //Console.WriteLine($"Unable to find property mapping for: {e}");
                    return list;
            }
        }

        public static void RestoreAllEffects(GamePlayer p)
        {
            GamePlayer player = p;

            if (player == null || player.DBCharacter == null || GameServer.Database == null)
                return;

            IList<PlayerXEffect> effs = DOLDB<PlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
            if (effs == null)
                return;

            foreach (PlayerXEffect eff in effs)
                GameServer.Database.DeleteObject(eff);

            foreach (PlayerXEffect eff in effs.GroupBy(e => e.Var1).Select(e => e.First()))
            {
                if (eff.SpellLine == GlobalSpellsLines.Reserved_Spells)
                    continue;

                bool good = true;
                Spell spell = SkillBase.GetSpellByID(eff.Var1);

                if (spell == null)
                    good = false;

                SpellLine line = null;

                if (!Util.IsEmpty(eff.SpellLine))
                {
                    line = SkillBase.GetSpellLine(eff.SpellLine, false);

                    if (line == null)
                        good = false;
                }
                else
                    good = false;

                if (good)
                {
                    ISpellHandler handler = ScriptMgr.CreateSpellHandler(player, spell, line);
                    handler.Spell.Duration = eff.Duration;
                    handler.Spell.CastTime = 1;
                    handler.StartSpell(player);
                    player.Out.SendStatusUpdate();
                }
            }
        }

        /// <summary>
        /// Save All Effect to PlayerXEffect Data Table
        /// </summary>
        public static void SaveAllEffects(GamePlayer player)
        {
            if (player == null || player.effectListComponent.GetAllEffects().Count == 0)
                return;

            IList<PlayerXEffect> effs = DOLDB<PlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
            if (effs != null)
                GameServer.Database.DeleteObject(effs);

            lock (player.effectListComponent.EffectsLock)
            {
                foreach (ECSGameEffect eff in player.effectListComponent.GetAllEffects())
                {
                    try
                    {
                        if (eff is ECSGameSpellEffect gse)
                        {
                            // No concentration Effect from other casters.
                            if (gse.SpellHandler?.Spell?.Concentration > 0 && gse.SpellHandler.Caster != player)
                                continue;
                        }

                        PlayerXEffect effx = eff.getSavedEffect();

                        if (effx == null)
                            continue;

                        if (effx.SpellLine == GlobalSpellsLines.Reserved_Spells)
                            continue;

                        effx.ChardID = player.ObjectId;

                        GameServer.Database.AddObject(effx);
                    }
                    catch (Exception e)
                    {
                        if (log.IsWarnEnabled)
                            log.WarnFormat("Could not save effect ({0}) on player: {1}, {2}", eff, player, e);
                    }
                }
            }
        }
    }
}
