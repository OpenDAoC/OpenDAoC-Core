using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.ServerProperties;
using DOL.GS.Spells;
using DOL.Logging;

namespace DOL.GS
{
    public static class EffectHelper
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public const int MAX_PROPERTIES_PER_EFFECT = 6; // Must be at least equal to the maximum number of properties that can be applied by a single effect.

        public static bool IsWithinConcentrationBuffRadius(GameLiving effectOwner, GameLiving effectSource, eSpellType spellType)
        {
            int radius = spellType is eSpellType.EnduranceRegenBuff ?
                Properties.ENDURANCE_CONCENTRATION_BUFF_RANGE :
                Properties.CONCENTRATION_BUFF_RANGE;

            return radius == 0 || effectOwner.IsWithinRadius(effectSource, radius);
        }

        public static void SendSpellAnimation(ECSGameSpellEffect e)
        {
            if (e == null)
                return;

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

        public static eEffect GetEffectFromSpell(Spell spell)
        {
            switch (spell.SpellType)
            {
                #region Positive Effects

                case eSpellType.Bladeturn:
                    return eEffect.Bladeturn;
                case eSpellType.DamageAdd:
                    return eEffect.DamageAdd;
                case eSpellType.DamageShield: // FocusShield: Could be the wrong SpellType here.
                    return eEffect.FocusShield;
                case eSpellType.AblativeArmor:
                    return eEffect.AblativeArmor;
                case eSpellType.MeleeDamageBuff:
                case eSpellType.SavageDPSBuff:
                    return eEffect.MeleeDamageBuff;
                case eSpellType.CombatSpeedBuff:
                case eSpellType.SavageCombatSpeedBuff:
                    return eEffect.MeleeHasteBuff;
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
                case eSpellType.BaseArmorFactorBuff:
                    return eEffect.BaseAFBuff;
                case eSpellType.SpecArmorFactorBuff:
                    return eEffect.SpecAFBuff;
                case eSpellType.PaladinArmorFactorBuff:
                    return eEffect.PaladinAf;
                case eSpellType.ArmorAbsorptionBuff:
                    return eEffect.PhysicalAbsorptionBuff; // Every ABS buff are applied as secondary ABS buff and don't modify armor ABS.
                case eSpellType.SavageEvadeBuff:
                    return eEffect.EvadeBuff;
                case eSpellType.SavageStyleEvadeBuff:
                    return eEffect.SavageStyleEvadeBuff;
                case eSpellType.SavageParryBuff:
                    return eEffect.ParryBuff;
                case eSpellType.SavageStyleParryBuff:
                    return eEffect.SavageStyleParryBuff;

                // Resists.
                case eSpellType.CrushResistBuff:
                case eSpellType.SavageCrushResistanceBuff:
                    return eEffect.CrushResistBuff;
                case eSpellType.SlashResistBuff:
                case eSpellType.SavageSlashResistanceBuff:
                    return eEffect.SlashResistBuff;
                case eSpellType.ThrustResistBuff:
                case eSpellType.SavageThrustResistanceBuff:
                    return eEffect.ThrustResistBuff;
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
                case eSpellType.PreventFlight:
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
                case eSpellType.Mesmerize:
                    return eEffect.Mez;
                case eSpellType.MesmerizeDurationBuff:
                    return eEffect.MesmerizeDurationBuff;
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
                case eSpellType.AcuityDebuff:
                    return eEffect.AcuityDebuff;
                case eSpellType.ArmorFactorDebuff:
                    return eEffect.ArmorFactorDebuff;
                case eSpellType.ArmorAbsorptionDebuff:
                    return eEffect.ArmorAbsorptionDebuff;

                // Resists.
                case eSpellType.CrushResistDebuff:
                    return eEffect.CrushResistDebuff;
                case eSpellType.SlashResistDebuff:
                    return eEffect.SlashResistDebuff;
                case eSpellType.ThrustResistDebuff:
                    return eEffect.ThrustResistDebuff;
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

                // Misc.
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
                case eSpellType.UnbreakableSpeedDecrease:
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

        public static int FillPropertiesFromEffect(eEffect effect, Span<eProperty> properties)
        {
            if (properties.Length < MAX_PROPERTIES_PER_EFFECT)
                throw new ArgumentException($"The properties span must have a length of at least {MAX_PROPERTIES_PER_EFFECT}.", nameof(properties));

            int count = 0;

            switch (effect)
            {
                case eEffect.StrengthBuff:
                case eEffect.StrengthDebuff:
                    properties[count++] = eProperty.Strength;
                    break;
                case eEffect.DexterityBuff:
                case eEffect.DexterityDebuff:
                    properties[count++] = eProperty.Dexterity;
                    break;
                case eEffect.ConstitutionBuff:
                case eEffect.ConstitutionDebuff:
                    properties[count++] = eProperty.Constitution;
                    break;
                case eEffect.AcuityBuff:
                case eEffect.AcuityDebuff:
                    properties[count++] = eProperty.Acuity;
                    break;
                case eEffect.StrengthConBuff:
                case eEffect.StrConDebuff:
                    properties[count++] = eProperty.Strength;
                    properties[count++] = eProperty.Constitution;
                    break;
                case eEffect.WsConDebuff:
                    properties[count++] = eProperty.WeaponSkill;
                    properties[count++] = eProperty.Constitution;
                    break;
                case eEffect.DexQuickBuff:
                case eEffect.DexQuiDebuff:
                    properties[count++] = eProperty.Dexterity;
                    properties[count++] = eProperty.Quickness;
                    break;
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorFactorDebuff:
                    properties[count++] = eProperty.ArmorFactor;
                    break;
                case eEffect.ArmorAbsorptionDebuff:
                    properties[count++] = eProperty.ArmorAbsorption;
                    break;
                case eEffect.PhysicalAbsorptionBuff:
                    properties[count++] = eProperty.PhysicalAbsorption;
                    break;
                case eEffect.MeleeDamageBuff:
                case eEffect.MeleeDamageDebuff:
                    properties[count++] = eProperty.MeleeDamage;
                    break;
                case eEffect.NaturalResistDebuff:
                    properties[count++] = eProperty.Resist_Natural;
                    break;
                case eEffect.BodyResistBuff:
                case eEffect.BodyResistDebuff:
                    properties[count++] = eProperty.Resist_Body;
                    break;
                case eEffect.SpiritResistBuff:
                case eEffect.SpiritResistDebuff:
                    properties[count++] = eProperty.Resist_Spirit;
                    break;
                case eEffect.EnergyResistBuff:
                case eEffect.EnergyResistDebuff:
                    properties[count++] = eProperty.Resist_Energy;
                    break;
                case eEffect.HeatResistBuff:
                case eEffect.HeatResistDebuff:
                    properties[count++] = eProperty.Resist_Heat;
                    break;
                case eEffect.ColdResistBuff:
                case eEffect.ColdResistDebuff:
                    properties[count++] = eProperty.Resist_Cold;
                    break;
                case eEffect.MatterResistBuff:
                case eEffect.MatterResistDebuff:
                    properties[count++] = eProperty.Resist_Matter;
                    break;
                case eEffect.HeatColdMatterBuff:
                    properties[count++] = eProperty.Resist_Heat;
                    properties[count++] = eProperty.Resist_Cold;
                    properties[count++] = eProperty.Resist_Matter;
                    break;
                case eEffect.BodySpiritEnergyBuff:
                    properties[count++] = eProperty.Resist_Body;
                    properties[count++] = eProperty.Resist_Spirit;
                    properties[count++] = eProperty.Resist_Energy;
                    break;
                case eEffect.AllMagicResistsBuff:
                    properties[count++] = eProperty.Resist_Body;
                    properties[count++] = eProperty.Resist_Spirit;
                    properties[count++] = eProperty.Resist_Energy;
                    properties[count++] = eProperty.Resist_Heat;
                    properties[count++] = eProperty.Resist_Cold;
                    properties[count++] = eProperty.Resist_Matter;
                    break;
                case eEffect.SlashResistBuff:
                case eEffect.SlashResistDebuff:
                    properties[count++] = eProperty.Resist_Slash;
                    break;
                case eEffect.ThrustResistBuff:
                case eEffect.ThrustResistDebuff:
                    properties[count++] = eProperty.Resist_Thrust;
                    break;
                case eEffect.CrushResistBuff:
                case eEffect.CrushResistDebuff:
                    properties[count++] = eProperty.Resist_Crush;
                    break;
                case eEffect.AllMeleeResistsBuff:
                case eEffect.AllMeleeResistsDebuff:
                    properties[count++] = eProperty.Resist_Crush;
                    properties[count++] = eProperty.Resist_Thrust;
                    properties[count++] = eProperty.Resist_Slash;
                    break;
                case eEffect.HealthRegenBuff:
                    properties[count++] = eProperty.HealthRegenerationAmount;
                    break;
                case eEffect.PowerRegenBuff:
                    properties[count++] = eProperty.PowerRegenerationAmount;
                    break;
                case eEffect.EnduranceRegenBuff:
                    properties[count++] = eProperty.EnduranceRegenerationAmount;
                    break;
                case eEffect.MeleeHasteBuff:
                case eEffect.MeleeHasteDebuff:
                    properties[count++] = eProperty.MeleeSpeed;
                    break;
                case eEffect.MovementSpeedBuff:
                case eEffect.MovementSpeedDebuff:
                    properties[count++] = eProperty.MaxSpeed;
                    break;
                case eEffect.MesmerizeDurationBuff:
                    properties[count++] = eProperty.MesmerizeDurationReduction;
                    break;
                case eEffect.FatigueConsumptionBuff:
                case eEffect.FatigueConsumptionDebuff:
                    properties[count++] = eProperty.FatigueConsumption;
                    break;
                case eEffect.EvadeBuff:
                case eEffect.SavageStyleEvadeBuff:
                    properties[count++] = eProperty.EvadeChance;
                    break;
                case eEffect.ParryBuff:
                case eEffect.SavageStyleParryBuff:
                    properties[count++] = eProperty.ParryChance;
                    break;
                default:
                    break;
            }

            return count;
        }

        public static PlayerUpdate GetPlayerUpdateFromEffect(eEffect effect)
        {
            // Doesn't set PlayerUpdate.CONCENTRATION.
            PlayerUpdate playerUpdate = PlayerUpdate.Icons;

            switch (effect)
            {
                case eEffect.StrengthBuff:
                case eEffect.StrengthDebuff:
                case eEffect.Disease:
                {
                    playerUpdate |= PlayerUpdate.Stats;
                    playerUpdate |= PlayerUpdate.Encumbrance;
                    playerUpdate |= PlayerUpdate.WeaponArmor;
                    break;
                }
                case eEffect.StrengthConBuff:
                case eEffect.StrConDebuff:
                {
                    playerUpdate |= PlayerUpdate.Stats;
                    playerUpdate |= PlayerUpdate.Encumbrance;
                    playerUpdate |= PlayerUpdate.WeaponArmor;
                    break;
                }
                case eEffect.ConstitutionBuff:
                case eEffect.ConstitutionDebuff:
                case eEffect.WsConDebuff:
                {
                    playerUpdate |= PlayerUpdate.Stats;
                    break;
                }
                case eEffect.DexterityBuff:
                case eEffect.DexterityDebuff:
                case eEffect.QuicknessBuff:
                case eEffect.QuicknessDebuff:
                {
                    playerUpdate |= PlayerUpdate.Stats;
                    break;
                }
                case eEffect.DexQuickBuff:
                case eEffect.DexQuiDebuff:
                {
                    playerUpdate |= PlayerUpdate.Stats;
                    playerUpdate |= PlayerUpdate.WeaponArmor;
                    break;
                }
                case eEffect.AcuityBuff:
                case eEffect.AcuityDebuff:
                {
                    playerUpdate |= PlayerUpdate.Stats;
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
                    playerUpdate |= PlayerUpdate.Resists;
                    break;
                }
                case eEffect.BaseAFBuff:
                case eEffect.SpecAFBuff:
                case eEffect.PaladinAf:
                case eEffect.ArmorFactorDebuff:
                {
                    playerUpdate |= PlayerUpdate.WeaponArmor;
                    break;
                }
            }

            return playerUpdate;
        }

        public static void RestoreAllEffects(GamePlayer player)
        {
            IList<DbPlayerXEffect> savedEffects = DOLDB<DbPlayerXEffect>.SelectObjects(DB.Column("ChardID").IsEqualTo(player.ObjectId));

            if (savedEffects == null)
                return;

            GameServer.Database.DeleteObject(savedEffects);

            foreach (DbPlayerXEffect savedEffect in savedEffects)
            {
                Spell spell = SkillBase.GetSpellByID(savedEffect.Var1);

                if (spell == null)
                    continue;

                SpellLine line = SkillBase.GetSpellLine(savedEffect.SpellLine, false);

                if (line == null)
                    continue;

                ISpellHandler handler = ScriptMgr.CreateSpellHandler(player, spell, line);
                handler.Spell.Duration = savedEffect.Duration;
                handler.StartSpell(player);
            }
        }

        public static void SaveAllEffects(GamePlayer player)
        {
            foreach (ECSGameSpellEffect effect in player.effectListComponent.GetSpellEffects())
            {
                try
                {
                    // Don't save effects from other players, as we won't be able to restore them correctly (different caster, dynamically scaled spell...)
                    if (effect.SpellHandler.Caster != player)
                        continue;

                    DbPlayerXEffect savedEffect = effect.GetSavedEffect();

                    if (savedEffect == null)
                        continue;

                    savedEffect.ChardID = player.ObjectId;
                    GameServer.Database.AddObject(savedEffect);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Could not save effect (Effect: {effect}) (Player: {player})", e);
                }
            }
        }

        [Flags]
        public enum PlayerUpdate : ushort
        {
            PetWindow =     1 << 7,
            Icons =         1 << 6,
            Stats =         1 << 5,
            Resists =       1 << 4,
            WeaponArmor =   1 << 3,
            Encumbrance =   1 << 2,
            Concentration = 1,
            None =          0
        }
    }
}
