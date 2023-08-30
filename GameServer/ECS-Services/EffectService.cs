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
                        HandleStartEffect(effect);

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

        private static void HandleStartEffect(ECSGameEffect effect)
        {
            effect.Owner.effectListComponent.StartAddEffect(effect);
        }

        public static void UpdateEffectIcons(ECSGameEffect effect)
        {
            if (effect.Owner is GamePlayer player)
            {
                List<ECSGameEffect> effectsToUpdate = new();

                if (effect.PreviousPosition >= 0)
                {
                    List<ECSGameEffect> effects = effect.Owner.effectListComponent.GetAllEffects();
                    effectsToUpdate.AddRange(effects.Skip(effect.PreviousPosition));
                }
                else
                {
                    // Fix for Buff Pot Barrel not showing all icons when used.
                    if (effect is ECSGameSpellEffect spellEffect && AllStatsBarrel.BuffList.Contains(spellEffect.SpellHandler.Spell.ID))
                    {
                        List<ECSGameEffect> effects = effect.Owner.effectListComponent.GetAllEffects();
                        effectsToUpdate.AddRange(effects.Skip(effects.Count - AllStatsBarrel.BuffList.Count));
                    }
                    // Fix for Regen Pot not showing all icons when used.
                    else if (effect is ECSGameSpellEffect regenEffect && AllRegenBuff.RegenList.Contains(regenEffect.SpellHandler.Spell.ID))
                    {
                        List<ECSGameEffect> effects = effect.Owner.effectListComponent.GetAllEffects();
                        effectsToUpdate.AddRange(effects.Skip(effects.Count - AllRegenBuff.RegenList.Count));
                    }
                    else
                        effectsToUpdate.Add(effect);
                }

                player.Out.SendUpdateIcons(effectsToUpdate, ref effect.Owner.effectListComponent.GetLastUpdateEffectsCount());
                SendPlayerUpdates(player);
                player.Out.SendConcentrationList();
            }
            else if (effect.Owner is GameNPC npc)
            {
                if (npc.Brain is IControlledBrain npcBrain)
                {
                    npcBrain.UpdatePetWindow();

                    if (npc is NecromancerPet)
                        SendPlayerUpdates(npcBrain.Owner as GamePlayer);
                }
            }
        }

        private static void HandleCancelEffect(ECSGameEffect effect)
        {
            if (!effect.Owner.effectListComponent.RemoveEffect(effect))
                return;

            if (effect is ECSGameSpellEffect spellEffect)
            {
                if (spellEffect.IsBuffActive && spellEffect.EffectType != eEffect.Pulse && spellEffect is not ECSImmunityEffect)
                    effect.OnStopEffect();

                effect.IsBuffActive = false;

                // Update the Concentration List if Conc Buff/Song/Chant.
                if (effect.CancelEffect && effect.ShouldBeRemovedFromConcentrationList())
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
                effect.OnStopEffect();

            effect.TryApplyImmunity();

            if (!effect.IsDisabled && effect.Owner.effectListComponent._effects.ContainsKey(effect.EffectType))
            {
                ECSGameSpellEffect enableEffect = effect.Owner.effectListComponent.GetSpellEffects(effect.EffectType).OrderByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
                if (enableEffect != null && enableEffect.IsDisabled)
                    RequestEnableEffect(enableEffect);
            }

            if (effect.Owner is GamePlayer player)
            {
                SendPlayerUpdates(player);

                List<ECSGameEffect> ecsList = new();
                List<ECSGameEffect> playerEffects = effect.Owner.effectListComponent.GetAllEffects();
                ecsList.AddRange(playerEffects.Skip(playerEffects.IndexOf(effect)));

                player.Out.SendUpdateIcons(ecsList, ref effect.Owner.effectListComponent.GetLastUpdateEffectsCount());
                player.Out.SendConcentrationList();
            }
            else if (effect.Owner is GameNPC npc && npc.Brain is IControlledBrain npcBrain)
                npcBrain.UpdatePetWindow();
        }

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

        public static void RequestCancelConcEffect(IConcentrationEffect concEffect, bool playerCanceled = false)
        {
            if (concEffect is ECSGameSpellEffect effect)
            {
                if (effect.SpellHandler.Spell.IsPulsing)
                    effect.Owner.ActivePulseSpells.TryRemove(effect.SpellHandler.Spell.SpellType, out Spell _);

                RequestCancelEffect(effect, playerCanceled);
            }
        }

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

        public static void RequestImmediateCancelConcEffect(IConcentrationEffect effect, bool playerCanceled = false)
        {
            if (effect is ECSGameSpellEffect gameSpellEffect)
            {
                RequestImmediateCancelEffect(gameSpellEffect, playerCanceled);

                if (gameSpellEffect.SpellHandler.Spell.IsPulsing)
                    gameSpellEffect.Owner.ActivePulseSpells.TryRemove(gameSpellEffect.SpellHandler.Spell.SpellType, out Spell _);
            }
        }

        public static void RequestStartEffect(ECSGameEffect effect)
        {
            HandleStartEffect(effect);
        }

        public static void RequestDisableEffect(ECSGameEffect effect)
        {
            effect.IsDisabled = true;
            effect.RenewEffect = false;
            HandleCancelEffect(effect);
        }

        public static void RequestEnableEffect(ECSGameEffect effect)
        {
            if (!effect.IsDisabled)
                return;

            effect.IsDisabled = false;
            effect.RenewEffect = true;
            HandleStartEffect(effect);
        }

        public static void SendSpellAnimation(ECSGameSpellEffect effect)
        {
            if (effect != null)
            {
                ISpellHandler spellHandler = effect.SpellHandler;
                Spell spell = spellHandler.Spell;
                GameLiving target;

                // Focus damage shield. Need to figure out why this is needed.
                if (spell.IsPulsing && spell.SpellType == eSpellType.DamageShield)
                    target = spellHandler.Target;
                else
                    target = effect.Owner;

                foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
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

        public static void SendSpellResistAnimation(ECSGameSpellEffect effect)
        {
            if (effect is null)
                return;

            foreach (GamePlayer player in effect.Owner.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                player.Out.SendSpellEffectAnimation(effect.SpellHandler.Caster, effect.Owner, effect.SpellHandler.Spell.ClientEffect, 0, false, 0);
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

            IList<PlayerXEffect> dbEffects = DOLDB<PlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));

            if (dbEffects == null)
                return;

            foreach (PlayerXEffect eff in dbEffects)
                GameServer.Database.DeleteObject(eff);

            foreach (PlayerXEffect dbEffect in dbEffects.GroupBy(e => e.Var1).Select(e => e.First()))
            {
                if (dbEffect.SpellLine == GlobalSpellsLines.Reserved_Spells)
                    continue;

                bool good = true;
                Spell spell = SkillBase.GetSpellByID(dbEffect.Var1);

                if (spell == null)
                    good = false;

                SpellLine line = null;

                if (!Util.IsEmpty(dbEffect.SpellLine))
                {
                    line = SkillBase.GetSpellLine(dbEffect.SpellLine, false);

                    if (line == null)
                        good = false;
                }
                else
                    good = false;

                if (good)
                {
                    ISpellHandler handler = ScriptMgr.CreateSpellHandler(player, spell, line);
                    handler.Spell.Duration = dbEffect.Duration;
                    handler.Spell.CastTime = 1;
                    handler.StartSpell(player);
                    player.Out.SendStatusUpdate();
                }
            }
        }

        public static void SaveAllEffects(GamePlayer player)
        {
            if (player == null || player.effectListComponent.GetAllEffects().Count == 0)
                return;

            IList<PlayerXEffect> dbEffects = DOLDB<PlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));

            if (dbEffects != null)
                GameServer.Database.DeleteObject(dbEffects);

            foreach (ECSGameEffect effect in player.effectListComponent.GetAllEffects())
            {
                try
                {
                    if (effect is ECSGameSpellEffect gameSpellEffect)
                    {
                        // No concentration effects from other casters.
                        if (gameSpellEffect.SpellHandler?.Spell?.Concentration > 0 && gameSpellEffect.SpellHandler.Caster != player)
                            continue;
                    }

                    PlayerXEffect dbEffect = effect.getSavedEffect();

                    if (dbEffect == null)
                        continue;

                    if (dbEffect.SpellLine == GlobalSpellsLines.Reserved_Spells)
                        continue;

                    dbEffect.ChardID = player.ObjectId;

                    GameServer.Database.AddObject(dbEffect);
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.Warn($"Could not save effect ({effect}) on player ({player}). {e}");
                }
            }
        }
    }
}
