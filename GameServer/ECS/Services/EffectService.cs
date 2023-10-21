using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Core.AI.Brain;
using Core.Database;
using Core.Database.Tables;
using Core.GS.AI.Brains;
using Core.GS.Effects;
using Core.GS.PacketHandler;
using Core.GS.Spells;
using Core.Language;
using ECS.Debug;
using log4net;

namespace Core.GS
{
    public static class EffectService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(EffectService);

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<EcsGameEffect> list = EntityManager.UpdateAndGetAll<EcsGameEffect>(EEntityType.Effect, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                EcsGameEffect effect = list[i];

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
                    ServiceUtil.HandleServiceException(e, SERVICE_NAME, effect, effect.Owner);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void HandlePropertyModification(EcsGameEffect e)
        {
            if (e.Owner == null)
            {
                //Console.WriteLine($"Invalid target for Effect {e}");
                return;
            }

            EcsGameSpellEffect spellEffect = e as EcsGameSpellEffect;
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
                    SendSpellResistAnimation(e as EcsGameSpellEffect);
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
                if ((!spellEffect.IsBuffActive && !spellEffect.IsDisabled)
                    || spellEffect is SavageBuffEcsEffect)
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

                if (spell.IsPulsing)
                {
                    // This should allow the caster to see the effect of the first tick of a beneficial pulse effect, even when recasted before the existing effect expired.
                    // It means they can spam some spells, but I consider it a good feedback for the player (example: Paladin's endurance chant).
                    // It should also allow harmful effects to be played on the targets, but not the caster (example: Reaver's PBAEs -- the flames, not the waves).
                    // It should prevent double animations too (only checking 'IsHarmful' and 'RenewEffect' would make resist chants play twice).
                    if (spellEffect is EcsPulseEffect)
                    {
                        if (!spell.IsHarmful && spell.SpellType != ESpellType.Charm && !spellEffect.RenewEffect)
                            SendSpellAnimation(spellEffect);
                    }
                    else if (spell.IsHarmful)
                        SendSpellAnimation(spellEffect);
                }
                else if (spellEffect is not EcsImmunityEffect)
                    SendSpellAnimation(spellEffect);
                if (e is StatDebuffEcsSpellEffect && spell.CastTime == 0)
                    StatDebuffEcsSpellEffect.TryDebuffInterrupt(spell, e.OwnerPlayer, caster);
            }
            else
                e.OnStartEffect();

            UpdateEffectIcons(e);
        }

        private static void UpdateEffectIcons(EcsGameEffect e)
        {
            if (e.Owner is GamePlayer player)
            {
                List<EcsGameEffect> ecsList = new();

                if (e.PreviousPosition >= 0)
                {
                    List<EcsGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                    ecsList.AddRange(playerEffects.Skip(e.PreviousPosition));
                }
                else
                {
                    //fix for Buff Pot Barrel not showing all icons when used
                    if (e is EcsGameSpellEffect spellEffect && AllStatsBarrel.BuffList.Contains(spellEffect.SpellHandler.Spell.ID))
                    {
                        List<EcsGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(playerEffects.Count - AllStatsBarrel.BuffList.Count));
                    }
                    //fix for Regen Pot not showing all icons when used
                    else if (e is EcsGameSpellEffect regenEffect && AllRegenBuffSpell.RegenList.Contains(regenEffect.SpellHandler.Spell.ID))
                    {
                        List<EcsGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                        ecsList.AddRange(playerEffects.Skip(playerEffects.Count - AllRegenBuffSpell.RegenList.Count));
                    }
                    else
                        ecsList.Add(e);
                }

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent.GetLastUpdateEffectsCount());
                SendPlayerUpdates(player);
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNpc npc)
            {
                if (npc.Brain is IControlledBrain npcBrain)
                {
                    npcBrain.UpdatePetWindow();

                    if (npc is NecromancerPet)
                        SendPlayerUpdates(npcBrain.Owner as GamePlayer);
                }
            }
        }

        private static void HandleCancelEffect(EcsGameEffect e)
        {
            if (!e.Owner.effectListComponent.RemoveEffect(e))
                return;

            if (e is EcsGameSpellEffect spellEffect)
            {
                if (spellEffect.IsBuffActive && spellEffect.EffectType != EEffect.Pulse && spellEffect is not EcsImmunityEffect)
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
                            if (spellEffect is EcsPulseEffect)
                            {
                                for (int i = 0; i < spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects.Count; i++)
                                {
                                    if (spellEffect.SpellHandler.Caster.effectListComponent.ConcentrationEffects[i] is EcsPulseEffect)
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
                EcsGameSpellEffect enableEffect = e.Owner.effectListComponent.GetSpellEffects(e.EffectType).OrderByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
                if (enableEffect != null && enableEffect.IsDisabled)
                    RequestEnableEffect(enableEffect);
            }

            if (e.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);

                List<EcsGameEffect> ecsList = new();
                List<EcsGameEffect> playerEffects = e.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(e)));

                player.Out.SendUpdateIcons(ecsList, ref e.Owner.effectListComponent.GetLastUpdateEffectsCount());
                player.Out.SendConcentrationList();
            }
            else if (e.Owner is GameNpc npc && npc.Brain is IControlledBrain npcBrain)
                npcBrain.UpdatePetWindow();
        }

        /// <summary>
        /// Immediately cancels an ECSGameEffect.
        /// </summary>
        public static void RequestCancelEffect(EcsGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            if (effect is QuickCastEcsAbilityEffect quickCast)
            {
                quickCast.Cancel(true);
                return;
            }

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((!effect.HasPositiveEffect) || effect is EcsImmunityEffect))
            {
                if (effect.Owner is GamePlayer player)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

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
            if (concEffect is EcsGameSpellEffect effect)
            {
                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.ActivePulseSpells.TryRemove(effect.SpellHandler.Spell.SpellType, out Spell _);

                RequestCancelEffect(effect, playerCanceled);
            }
        }

        /// <summary>
        /// Immediately removes an ECSGameEffect.
        /// </summary>
        public static void RequestImmediateCancelEffect(EcsGameEffect effect, bool playerCanceled = false)
        {
            if (effect is null)
                return;

            // Player can't remove negative effect or Effect in Immunity State
            if (playerCanceled && ((!effect.HasPositiveEffect) || effect is EcsImmunityEffect))
            {
                if (effect.Owner is GamePlayer player)
                    player.Out.SendMessage(LanguageMgr.GetTranslation((effect.Owner as GamePlayer).Client, "Effects.GameSpellEffect.CantRemoveEffect"), EChatType.CT_System, EChatLoc.CL_SystemWindow);

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
            if (concEffect is EcsGameSpellEffect effect)
            {
                RequestImmediateCancelEffect(effect, playerCanceled);

                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.ActivePulseSpells.TryRemove(effect.SpellHandler.Spell.SpellType, out Spell _);
            }
        }

        /// <summary>
        /// Immediately starts an ECSGameEffect.
        /// </summary>
        public static void RequestStartEffect(EcsGameEffect effect)
        {
            HandlePropertyModification(effect);
        }

        /// <summary>
        /// Immediately disables an ECSGameEffect.
        /// </summary>
        public static void RequestDisableEffect(EcsGameEffect effect)
        {
            effect.IsDisabled = true;
            effect.RenewEffect = false;
            HandleCancelEffect(effect);
        }

        /// <summary>
        /// Immediately enables a previously disabled ECSGameEffect.
        /// </summary>
        public static void RequestEnableEffect(EcsGameEffect effect)
        {
            if (!effect.IsDisabled)
                return;

            effect.IsDisabled = false;
            effect.RenewEffect = true;
            HandlePropertyModification(effect);
        }

        public static void SendSpellAnimation(EcsGameSpellEffect e)
        {
            if (e != null)
            {
                ISpellHandler spellHandler = e.SpellHandler;
                Spell spell = spellHandler.Spell;
                GameLiving target;

                // Focus damage shield. Need to figure out why this is needed.
                if (spell.IsPulsing && spell.SpellType == ESpellType.DamageShield)
                    target = spellHandler.Target;
                else
                    target = e.Owner;

                foreach (GamePlayer player in e.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                    player.Out.SendSpellEffectAnimation(spellHandler.Caster, target, spell.ClientEffect, 0, false, 1);
            }
        }

        public static EEffect GetEffectFromSpell(Spell spell, bool isBaseLine = true)
        {
            switch (spell.SpellType)
            {
                #region Positive Effects

                case ESpellType.Bladeturn:
                    return EEffect.Bladeturn;
                case ESpellType.DamageAdd:
                    return EEffect.DamageAdd;
                //case eSpellType.DamageReturn:
                //    return eEffect.DamageReturn;
                case ESpellType.DamageShield: // FocusShield: Could be the wrong SpellType here.
                    return EEffect.FocusShield;
                case ESpellType.AblativeArmor:
                    return EEffect.AblativeArmor;
                case ESpellType.MeleeDamageBuff:
                    return EEffect.MeleeDamageBuff;
                case ESpellType.CombatSpeedBuff:
                    return EEffect.MeleeHasteBuff;
                //case eSpellType.Celerity: // Possibly the same as CombatSpeedBuff?
                //    return eEffect.Celerity;
                case ESpellType.SpeedOfTheRealm:
                case ESpellType.SpeedEnhancement:
                    return EEffect.MovementSpeedBuff;
                case ESpellType.HealOverTime:
                    return EEffect.HealOverTime;
                case ESpellType.CombatHeal:
                    return EEffect.CombatHeal;

                // Stats.
                case ESpellType.StrengthBuff:
                    return EEffect.StrengthBuff;
                case ESpellType.DexterityBuff:
                    return EEffect.DexterityBuff;
                case ESpellType.ConstitutionBuff:
                    return EEffect.ConstitutionBuff;
                case ESpellType.StrengthConstitutionBuff:
                    return EEffect.StrengthConBuff;
                case ESpellType.DexterityQuicknessBuff:
                    return EEffect.DexQuickBuff;
                case ESpellType.AcuityBuff:
                    return EEffect.AcuityBuff;
                case ESpellType.ArmorAbsorptionBuff:
                    return EEffect.ArmorAbsorptionBuff;
                case ESpellType.PaladinArmorFactorBuff:
                    return EEffect.PaladinAf;
                case ESpellType.ArmorFactorBuff:
                    return isBaseLine ? EEffect.BaseAFBuff : EEffect.SpecAFBuff;

                // Resists.
                case ESpellType.BodyResistBuff:
                    return EEffect.BodyResistBuff;
                case ESpellType.SpiritResistBuff:
                    return EEffect.SpiritResistBuff;
                case ESpellType.EnergyResistBuff:
                    return EEffect.EnergyResistBuff;
                case ESpellType.HeatResistBuff:
                    return EEffect.HeatResistBuff;
                case ESpellType.ColdResistBuff:
                    return EEffect.ColdResistBuff;
                case ESpellType.MatterResistBuff:
                    return EEffect.MatterResistBuff;
                case ESpellType.BodySpiritEnergyBuff:
                    return EEffect.BodySpiritEnergyBuff;
                case ESpellType.HeatColdMatterBuff:
                    return EEffect.HeatColdMatterBuff;
                case ESpellType.AllMagicResistBuff:
                    return EEffect.AllMagicResistsBuff;

                // Regens.
                case ESpellType.HealthRegenBuff:
                    return EEffect.HealthRegenBuff;
                case ESpellType.EnduranceRegenBuff:
                    return EEffect.EnduranceRegenBuff;
                case ESpellType.PowerRegenBuff:
                    return EEffect.PowerRegenBuff;

                // Misc.
                case ESpellType.OffensiveProc:
                    return EEffect.OffensiveProc;
                case ESpellType.DefensiveProc:
                    return EEffect.DefensiveProc;
                case ESpellType.HereticPiercingMagic:
                    return EEffect.HereticPiercingMagic;

                #endregion
                #region NEGATIVE_EFFECTS

                case ESpellType.StyleBleeding:
                    return EEffect.Bleed;
                case ESpellType.DamageOverTime:
                    return EEffect.DamageOverTime;
                case ESpellType.Charm:
                    return EEffect.Charm;
                case ESpellType.DamageSpeedDecrease:
                case ESpellType.DamageSpeedDecreaseNoVariance:
                case ESpellType.StyleSpeedDecrease:
                case ESpellType.SpeedDecrease:
                case ESpellType.UnbreakableSpeedDecrease:
                    return EEffect.MovementSpeedDebuff;
                case ESpellType.MeleeDamageDebuff:
                    return EEffect.MeleeDamageDebuff;
                case ESpellType.StyleCombatSpeedDebuff:
                case ESpellType.CombatSpeedDebuff:
                    return EEffect.MeleeHasteDebuff;
                case ESpellType.Disease:
                    return EEffect.Disease;
                case ESpellType.Confusion:
                    return EEffect.Confusion;

                // Crowd control.
                case ESpellType.StyleStun:
                case ESpellType.Stun:
                    return EEffect.Stun;
                //case eSpellType.StunImmunity:
                //    return eEffect.StunImmunity;
                case ESpellType.Mesmerize:
                    return EEffect.Mez;
                case ESpellType.MesmerizeDurationBuff:
                    return EEffect.MesmerizeDurationBuff;
                //case eSpellType.MezImmunity:
                //    return eEffect.MezImmunity;
                //case eSpellType.StyleSpeedDecrease:
                //    return eEffect.MeleeSnare;
                //case eSpellType.Snare: // May work off of SpeedDecrease.
                //    return eEffect.Snare;
                //case eSpellType.SnareImmunity: // Not implemented.
                //    return eEffect.SnareImmunity;
                case ESpellType.Nearsight:
                    return EEffect.Nearsight;

                // Stats.
                case ESpellType.StrengthDebuff:
                    return EEffect.StrengthDebuff;
                case ESpellType.DexterityDebuff:
                    return EEffect.DexterityDebuff;
                case ESpellType.ConstitutionDebuff:
                    return EEffect.ConstitutionDebuff;
                case ESpellType.StrengthConstitutionDebuff:
                    return EEffect.StrConDebuff;
                case ESpellType.DexterityQuicknessDebuff:
                    return EEffect.DexQuiDebuff;
                case ESpellType.WeaponSkillConstitutionDebuff:
                    return EEffect.WsConDebuff;
                //case eSpellType.AcuityDebuff: // Not sure what this is yet.
                //    return eEffect.Acuity;
                case ESpellType.ArmorAbsorptionDebuff:
                    return EEffect.ArmorAbsorptionDebuff;
                case ESpellType.ArmorFactorDebuff:
                    return EEffect.ArmorFactorDebuff;

                // Resists.
                case ESpellType.BodyResistDebuff:
                    return EEffect.BodyResistDebuff;
                case ESpellType.SpiritResistDebuff:
                    return EEffect.SpiritResistDebuff;
                case ESpellType.EnergyResistDebuff:
                    return EEffect.EnergyResistDebuff;
                case ESpellType.HeatResistDebuff:
                    return EEffect.HeatResistDebuff;
                case ESpellType.ColdResistDebuff:
                    return EEffect.ColdResistDebuff;
                case ESpellType.MatterResistDebuff:
                    return EEffect.MatterResistDebuff;
                case ESpellType.SlashResistDebuff:
                    return EEffect.SlashResistDebuff;

                // Misc.
                case ESpellType.SavageCombatSpeedBuff:
                    return EEffect.MeleeHasteBuff;
                case ESpellType.SavageCrushResistanceBuff:
                case ESpellType.SavageDPSBuff:
                case ESpellType.SavageEnduranceHeal:
                case ESpellType.SavageEvadeBuff:
                case ESpellType.SavageParryBuff:
                case ESpellType.SavageSlashResistanceBuff:
                case ESpellType.SavageThrustResistanceBuff:
                    return EEffect.SavageBuff;
                case ESpellType.DirectDamage:
                    return EEffect.DirectDamage;
                case ESpellType.FacilitatePainworking:
                    return EEffect.FacilitatePainworking;
                case ESpellType.FatigueConsumptionBuff:
                    return EEffect.FatigueConsumptionBuff;
                case ESpellType.FatigueConsumptionDebuff:
                    return EEffect.FatigueConsumptionDebuff;
                case ESpellType.DirectDamageWithDebuff:
                    if (spell.DamageType == EDamageType.Body)
                        return EEffect.BodyResistDebuff;
                    else if (spell.DamageType == EDamageType.Cold)
                        return EEffect.ColdResistDebuff;
                    else if (spell.DamageType == EDamageType.Heat)
                        return EEffect.HeatResistDebuff;
                    else
                        return EEffect.Unknown;
                case ESpellType.PiercingMagic:
                    return EEffect.PiercingMagic;
                case ESpellType.PveResurrectionIllness:
                    return EEffect.ResurrectionIllness;
                case ESpellType.RvrResurrectionIllness:
                    return EEffect.RvrResurrectionIllness;

                #endregion

                // Pets.
                case ESpellType.SummonTheurgistPet:
                case ESpellType.SummonNoveltyPet:
                case ESpellType.SummonAnimistPet:
                case ESpellType.SummonAnimistFnF:
                case ESpellType.SummonSpiritFighter:
                case ESpellType.SummonHunterPet:
                case ESpellType.SummonUnderhill:
                case ESpellType.SummonDruidPet:
                case ESpellType.SummonSimulacrum:
                case ESpellType.SummonNecroPet:
                case ESpellType.SummonCommander:
                case ESpellType.SummonMinion:
                    return EEffect.Pet;

                default:
                    //Console.WriteLine($"Unable to map effect for ECSGameEffect! {spell}");
                    return EEffect.Unknown;
            }
        }

        public static void SendSpellResistAnimation(EcsGameSpellEffect e)
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

        public static List<EProperty> GetPropertiesFromEffect(EEffect e)
        {
            List<EProperty> list = new();

            switch (e)
            {
                case EEffect.StrengthBuff:
                case EEffect.StrengthDebuff:
                    list.Add(EProperty.Strength);
                    return list;
                case EEffect.DexterityBuff:
                case EEffect.DexterityDebuff:
                    list.Add(EProperty.Dexterity);
                    return list;
                case EEffect.ConstitutionBuff:
                case EEffect.ConstitutionDebuff:
                    list.Add(EProperty.Constitution);
                    return list;
                case EEffect.AcuityBuff:
                case EEffect.AcuityDebuff:
                    list.Add(EProperty.Acuity);
                    return list;
                case EEffect.StrengthConBuff:
                case EEffect.StrConDebuff:
                    list.Add(EProperty.Strength);
                    list.Add(EProperty.Constitution);
                    return list;
                case EEffect.WsConDebuff:
                    list.Add(EProperty.WeaponSkill);
                    list.Add(EProperty.Constitution);
                    return list;
                case EEffect.DexQuickBuff:
                case EEffect.DexQuiDebuff:
                    list.Add(EProperty.Dexterity);
                    list.Add(EProperty.Quickness);
                    return list;
                case EEffect.BaseAFBuff:
                case EEffect.SpecAFBuff:
                case EEffect.PaladinAf:
                case EEffect.ArmorFactorDebuff:
                    list.Add(EProperty.ArmorFactor);
                    return list;
                case EEffect.ArmorAbsorptionBuff:
                case EEffect.ArmorAbsorptionDebuff:
                    list.Add(EProperty.ArmorAbsorption);
                    return list;
                case EEffect.MeleeDamageBuff:
                case EEffect.MeleeDamageDebuff:
                    list.Add(EProperty.MeleeDamage);
                    return list;
                case EEffect.NaturalResistDebuff:
                    list.Add(EProperty.Resist_Natural);
                    return list;
                case EEffect.BodyResistBuff:
                case EEffect.BodyResistDebuff:
                    list.Add(EProperty.Resist_Body);
                    return list;
                case EEffect.SpiritResistBuff:
                case EEffect.SpiritResistDebuff:
                    list.Add(EProperty.Resist_Spirit);
                    return list;
                case EEffect.EnergyResistBuff:
                case EEffect.EnergyResistDebuff:
                    list.Add(EProperty.Resist_Energy);
                    return list;
                case EEffect.HeatResistBuff:
                case EEffect.HeatResistDebuff:
                    list.Add(EProperty.Resist_Heat);
                    return list;
                case EEffect.ColdResistBuff:
                case EEffect.ColdResistDebuff:
                    list.Add(EProperty.Resist_Cold);
                    return list;
                case EEffect.MatterResistBuff:
                case EEffect.MatterResistDebuff:
                    list.Add(EProperty.Resist_Matter);
                    return list;
                case EEffect.HeatColdMatterBuff:
                    list.Add(EProperty.Resist_Heat);
                    list.Add(EProperty.Resist_Cold);
                    list.Add(EProperty.Resist_Matter);
                    return list;
                case EEffect.BodySpiritEnergyBuff:
                    list.Add(EProperty.Resist_Body);
                    list.Add(EProperty.Resist_Spirit);
                    list.Add(EProperty.Resist_Energy);
                    return list;
                case EEffect.AllMagicResistsBuff:
                    list.Add(EProperty.Resist_Body);
                    list.Add(EProperty.Resist_Spirit);
                    list.Add(EProperty.Resist_Energy);
                    list.Add(EProperty.Resist_Heat);
                    list.Add(EProperty.Resist_Cold);
                    list.Add(EProperty.Resist_Matter);
                    return list;
                case EEffect.SlashResistBuff:
                case EEffect.SlashResistDebuff:
                    list.Add(EProperty.Resist_Slash);
                    return list;
                case EEffect.ThrustResistBuff:
                case EEffect.ThrustResistDebuff:
                    list.Add(EProperty.Resist_Thrust);
                    return list;
                case EEffect.CrushResistBuff:
                case EEffect.CrushResistDebuff:
                    list.Add(EProperty.Resist_Crush);
                    return list;
                case EEffect.AllMeleeResistsBuff:
                case EEffect.AllMeleeResistsDebuff:
                    list.Add(EProperty.Resist_Crush);
                    list.Add(EProperty.Resist_Thrust);
                    list.Add(EProperty.Resist_Slash);
                    return list;
                case EEffect.HealthRegenBuff:
                    list.Add(EProperty.HealthRegenerationRate);
                    return list;
                case EEffect.PowerRegenBuff:
                    list.Add(EProperty.PowerRegenerationRate);
                    return list;
                case EEffect.EnduranceRegenBuff:
                    list.Add(EProperty.EnduranceRegenerationRate);
                    return list;
                case EEffect.MeleeHasteBuff:
                case EEffect.MeleeHasteDebuff:
                    list.Add(EProperty.MeleeSpeed);
                    return list;
                case EEffect.MovementSpeedBuff:
                case EEffect.MovementSpeedDebuff:
                    list.Add(EProperty.MaxSpeed);
                    return list;
                case EEffect.MesmerizeDurationBuff:
                    list.Add(EProperty.MesmerizeDurationReduction);
                    return list;
                case EEffect.FatigueConsumptionBuff:
                case EEffect.FatigueConsumptionDebuff:
                    list.Add(EProperty.FatigueConsumption);
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

            IList<DbPlayerXEffect> effs = CoreDb<DbPlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
            if (effs == null)
                return;

            foreach (DbPlayerXEffect eff in effs)
                GameServer.Database.DeleteObject(eff);

            foreach (DbPlayerXEffect eff in effs.GroupBy(e => e.Var1).Select(e => e.First()))
            {
                if (eff.SpellLine == GlobalSpellsLines.Reserved_Spells)
                    continue;

                bool good = true;
                Spell spell = SkillBase.GetSpellByID(eff.Var1);

                if (spell == null)
                    good = false;

                SpellLine line = null;

                if (!string.IsNullOrEmpty(eff.SpellLine))
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

            IList<DbPlayerXEffect> effs = CoreDb<DbPlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
            if (effs != null)
                GameServer.Database.DeleteObject(effs);

            lock (player.effectListComponent.EffectsLock)
            {
                foreach (EcsGameEffect eff in player.effectListComponent.GetAllEffects())
                {
                    try
                    {
                        if (eff is EcsGameSpellEffect gse)
                        {
                            // No concentration Effect from other casters.
                            if (gse.SpellHandler?.Spell?.Concentration > 0 && gse.SpellHandler.Caster != player)
                                continue;
                        }

                        DbPlayerXEffect effx = eff.getSavedEffect();

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
