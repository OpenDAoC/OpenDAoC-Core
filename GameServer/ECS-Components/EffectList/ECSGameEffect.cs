using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSGameEffect
    {
        public ISpellHandler SpellHandler;
        //Based on GameLoop expire tick
        public long ExpireTick;
        public long Duration;
        public long PulseFreq;
        public double Effectiveness;
        public ushort Icon;
        public bool CancelEffect;
        public eEffect EffectType;
        public eSpellType SpellType;
        public GameLiving Owner;
        
        
        public ECSGameEffect(GameLiving owner,ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
        {
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            ExpireTick = 0;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            Icon = icon;
            CancelEffect = cancelEffect;
            EffectType = MapEffect();
        }

        public ushort GetRemainingTimeForClient()
        {
            return 10000;
        }

        private eEffect MapEffect()
        {
            Console.WriteLine("Spell of type: " + SpellHandler.Spell.SpellType);
            switch (SpellHandler.Spell.SpellType)
            {
                //stat buffs
                case "StrengthBuff":
                    return eEffect.StrengthBuff;
                case "DexterityBuff":
                    return eEffect.DexterityBuff;
                case "ConstitutionBuff":
                    return eEffect.ConstitutionBuff;
                case "QuicknessBuff":
                    return eEffect.QuicknessBuff;
                case "StrengthConstitutionBuff":
                    return eEffect.StrengthConBuff;
                case "DexterityQuicknessBuff":
                    return eEffect.DexQuickBuff;
                case "AcuityBuff":
                    return eEffect.AcuityBuff;
                case "ArmorAbsorptionBuff":
                    return eEffect.ArmorAbsorptionBuff;
                case "ArmorFactorBuff":
                    return eEffect.BaseAFBuff; //currently no map to specAF. where is spec AF handled?
                case "PaladinArmorFactorBuff":
                    return eEffect.SpecAFBuff;

                //resist buffs
                case "BodyResistBuff":
                    return eEffect.BodyResistBuff;
                case "SpiritResistBuff":
                    return eEffect.SpiritResistBuff;
                case "EnergyResistBuff":
                    return eEffect.EnergyResistBuff;
                case "HeatResistBuff":
                    return eEffect.HeatResistBuff;
                case "ColdResistBuff":
                    return eEffect.ColdResistBuff;
                case "MatterResistBuff":
                    return eEffect.MatterResistBuff;
                case "BodySpiritEnergyBuff":
                    return eEffect.BodySpiritEnergyBuff;
                case "HeatColdMatterBuff":
                    return eEffect.HeatColdMatterBuff;
                case "AllMagicResistsBuff":
                    return eEffect.AllMagicResistsBuff;
                case "CrushResistBuff":
                    return eEffect.CrushResistBuff;
                case "SlashResistBuff":
                    return eEffect.SlashResistBuff;
                case "ThrustResistBuff":
                    return eEffect.ThrustResistBuff;
                case "ResiPierceBuff":
                    return eEffect.Unknown;
                case "CrushSlashThrustBuff":
                case "AllMeleeResistsBuff":
                    return eEffect.AllMeleeResistsBuff;
                case "AllResistsBuff":
                    return eEffect.AllResistsBuff;

                //combat buffs
                case "CombatSpeedBuff":
                    return eEffect.Unknown;
                case "HasteBuff":
                    return eEffect.Unknown;
                case "CelerityBuff":
                    return eEffect.Unknown;
                case "FatigueConsumptionBuff":
                    return eEffect.Unknown;
                case "MeleeDamageBuff":
                    return eEffect.Unknown;
                case "MesmerizeDurationBuff":
                    return eEffect.Unknown;
                case "DPSBuff":
                    return eEffect.Unknown;
                case "ToHitBuff":
                    return eEffect.Unknown;
                case "MagicResistsBuff":
                    return eEffect.Unknown;
                case "StyleAbsorbBuff":
                    return eEffect.Unknown;
                case "ExtraHP":
                    return eEffect.Unknown;

                //skill buffs
                case "FlexibleSkillBuff":
                    return eEffect.Unknown;
                case "EvadeBuff":
                    return eEffect.Unknown;
                case "ParryBuff":
                    return eEffect.Unknown;
                case "WeaponSkillBuff":
                    return eEffect.Unknown;
                case "StealthSkillBuff":
                    return eEffect.Unknown;


                //stat debuffs
                case "StrengthDebuff":
                    return eEffect.StrengthDebuff;
                case "DexterityDebuff":
                    return eEffect.DexterityDebuff;
                case "ConstitutionDebuff":
                    return eEffect.ConstitutionDebuff;
                case "QuicknessDebuff":
                    return eEffect.QuicknessDebuff;
                case "AcuityDebuff":
                    return eEffect.AcuityDebuff;
                case "StrengthConstitutionDebuff":
                    return eEffect.StrConDebuff;
                case "DexterityQuicknessDebuff":
                    return eEffect.DexQuiDebuff;
                case "DexterityConstitutionDebuff":
                    return eEffect.Unknown; //add dex/con debuff
                case "WeaponSkillConstitutionDebuff":
                    return eEffect.Unknown; //add ws/con debuff
                case "ArmorFactorDebuff":
                    return eEffect.ArmorFactorDebuff;
                case "ArmorAbsorptionDebuff":
                    return eEffect.ArmorAbsorptionDebuff;

                //resist debuffs
                case "BodyResistDebuff":
                    return eEffect.BodyResistDebuff;
                case "ColdResistDebuff":
                    return eEffect.ColdResistDebuff;
                case "EnergyResistDebuff":
                    return eEffect.EnergyResistDebuff;
                case "HeatResistDebuff":
                    return eEffect.HeatResistDebuff;
                case "MatterResistDebuff":
                    return eEffect.MatterResistDebuff;
                case "SpiritResistDebuff":
                    return eEffect.SpiritResistDebuff;
                case "SlashResistDebuff":
                    return eEffect.SlashResistDebuff;
                case "ThrustResistDebuff":
                    return eEffect.ThrustResistDebuff; 
                case "CrushResistDebuff":
                    return eEffect.CrushResistDebuff;
                case "CrushSlashThrustDebuff":
                    return eEffect.AllMeleeResistsDebuff; //implement
                case "EssenceSear":
                    return eEffect.NaturalResistDebuff; //implement

                //combat debuffs
                case "CombatSpeedDebuff":
                    return eEffect.MeleeHasteDebuff;
                case "MeleeDamageDebuff":
                    return eEffect.MeleeDamageDebuff;
                case "FatigueConsumptionDebuff":
                    return eEffect.Unknown; //need to add fatigue consumption debuff to effects
                case "FumbleChanceDebuff":
                    return eEffect.Unknown; //need to add fumble chance debuff
                case "DPSDebuff":
                    return eEffect.MeleeDamageDebuff;
                case "ToHitDebuff":
                    return eEffect.Unknown; //add to hit debuff


                default:
                    Console.WriteLine($"Unable to map effect for ECSGameEffect! {this}");
                    return eEffect.Unknown;
            }
        }

    }
}