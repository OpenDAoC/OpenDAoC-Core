using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.Database;
using DOL.GS.Spells;

namespace DOL.GS
{
    public static class EffectHelper
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public static int GetConcentrationEffectActivationRange(eSpellType spellType)
        {
            return spellType is not eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500;
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

        public static eEffect GetEffectFromSpell(Spell spell)
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
                case eSpellType.BaseArmorFactorBuff:
                    return eEffect.BaseAFBuff;
                case eSpellType.SpecArmorFactorBuff:
                    return eEffect.SpecAFBuff;
                case eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;

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
                case eSpellType.AllSecondaryMagicResistsBuff:
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

                #region Negative Effects

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
                case eSpellType.SummonJuggernaut:
                case eSpellType.SummonAnimistAmbusher:
                    return eEffect.Pet;
                default:
                    return eEffect.Unknown;
            }
        }

        public static eEffect GetImmunityEffectFromSpell(Spell spell)
        {
            switch (spell.SpellType)
            {
                case eSpellType.Mesmerize:
                    return eEffect.MezImmunity;
                case eSpellType.StyleStun:
                case eSpellType.Stun:
                    return eEffect.StunImmunity;
                case eSpellType.SpeedDecrease:
                case eSpellType.DamageSpeedDecreaseNoVariance:
                case eSpellType.DamageSpeedDecrease:
                    return eEffect.SnareImmunity;
                case eSpellType.Nearsight:
                    return eEffect.NearsightImmunity;
                default:
                    return eEffect.Unknown;
            }
        }

        public static eEffect GetNpcImmunityEffectFromSpell(Spell spell)
        {
            switch (spell.SpellType)
            {
                case eSpellType.Mesmerize:
                    return eEffect.NPCMezImmunity;
                case eSpellType.StyleStun:
                case eSpellType.Stun:
                    return eEffect.NPCStunImmunity;
                default:
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
                    list.Add(eProperty.HealthRegenerationAmount);
                    return list;
                case eEffect.PowerRegenBuff:
                    list.Add(eProperty.PowerRegenerationAmount);
                    return list;
                case eEffect.EnduranceRegenBuff:
                    list.Add(eProperty.EnduranceRegenerationAmount);
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
                    return list;
            }
        }

        public static PlayerUpdate GetPlayerUpdateFromEffect(eEffect effect)
        {
            // Doesn't set PlayerUpdate.CONCENTRATION.
            PlayerUpdate playerUpdate = PlayerUpdate.ICONS;

            switch (effect)
            {
                case eEffect.StrengthBuff:
                case eEffect.StrengthDebuff:
                case eEffect.Disease:
                {
                    playerUpdate |= PlayerUpdate.STATS;
                    playerUpdate |= PlayerUpdate.ENCUMBERANCE;
                    break;
                }
                case eEffect.StrengthConBuff:
                case eEffect.StrConDebuff:
                {
                    playerUpdate |= PlayerUpdate.STATUS;
                    playerUpdate |= PlayerUpdate.STATS;
                    playerUpdate |= PlayerUpdate.ENCUMBERANCE;
                    break;
                }
                case eEffect.ConstitutionBuff:
                case eEffect.ConstitutionDebuff:
                case eEffect.WsConDebuff:
                {
                    playerUpdate |= PlayerUpdate.STATUS;
                    playerUpdate |= PlayerUpdate.STATS;
                    break;
                }
                case eEffect.DexterityBuff:
                case eEffect.DexterityDebuff:
                case eEffect.QuicknessBuff:
                case eEffect.QuicknessDebuff:
                case eEffect.DexQuickBuff:
                case eEffect.DexQuiDebuff:
                case eEffect.AcuityBuff:
                case eEffect.AcuityDebuff:
                {
                    playerUpdate |= PlayerUpdate.STATS;
                    break;
                }
                case eEffect.BodyResistBuff:
                case eEffect.BodyResistDebuff:
                case eEffect.SpiritResistBuff:
                case eEffect.SpiritResistDebuff:
                case eEffect.EnergyResistBuff:
                case eEffect.EnergyResistDebuff:
                case eEffect.HeatResistBuff:
                case eEffect.HeatResistDebuff:
                case eEffect.ColdResistBuff:
                case eEffect.ColdResistDebuff:
                case eEffect.MatterResistBuff:
                case eEffect.MatterResistDebuff:
                case eEffect.HeatColdMatterBuff:
                case eEffect.BodySpiritEnergyBuff:
                case eEffect.AllMagicResistsBuff:
                case eEffect.SlashResistBuff:
                case eEffect.SlashResistDebuff:
                case eEffect.ThrustResistBuff:
                case eEffect.ThrustResistDebuff:
                case eEffect.CrushResistBuff:
                case eEffect.CrushResistDebuff:
                case eEffect.AllMeleeResistsBuff:
                case eEffect.AllMeleeResistsDebuff:
                {
                    playerUpdate |= PlayerUpdate.RESISTS;
                    break;
                }
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorFactorDebuff:
                {
                    playerUpdate |= PlayerUpdate.WEAPON_ARMOR;
                    break;
                }
            }

            return playerUpdate;
        }

        public static void RestoreAllEffects(GamePlayer p)
        {
            GamePlayer player = p;

            if (player == null || player.DBCharacter == null || GameServer.Database == null)
                return;

            IList<DbPlayerXEffect> effs = DOLDB<DbPlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
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

        public static void SaveAllEffects(GamePlayer player)
        {
            IList<DbPlayerXEffect> effs = DOLDB<DbPlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));
            if (effs != null)
                GameServer.Database.DeleteObject(effs);

            foreach (ECSGameEffect eff in player.effectListComponent.GetEffects())
            {
                try
                {
                    if (eff is ECSGameSpellEffect gse)
                    {
                        // No concentration Effect from other casters.
                        if (gse.SpellHandler?.Spell?.Concentration > 0 && gse.SpellHandler.Caster != player)
                            continue;
                    }

                    DbPlayerXEffect effect = eff.GetSavedEffect();

                    if (effect == null)
                        continue;

                    if (effect.SpellLine == GlobalSpellsLines.Reserved_Spells)
                        continue;

                    effect.ChardID = player.ObjectId;

                    GameServer.Database.AddObject(effect);
                }
                catch (Exception e)
                {
                    if (log.IsWarnEnabled)
                        log.WarnFormat("Could not save effect ({0}) on player: {1}, {2}", eff, player, e);
                }
            }
        }

        [Flags]
        public enum PlayerUpdate : byte
        {
            ICONS =         1 << 7,
            STATUS =        1 << 6,
            STATS =         1 << 5,
            RESISTS =       1 << 4,
            WEAPON_ARMOR =  1 << 3,
            ENCUMBERANCE =  1 << 2,
            CONCENTRATION = 1,
            NONE =          0
        }
    }
}
