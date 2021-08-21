using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSGameEffect
    {
        public ISpellHandler SpellHandler;
        //Based on GameLoop expire tick
        public long ExpireTick;
        public long StartTick;
        public long LastTick;
        public long Duration;
        public long PulseFreq;
        public double Effectiveness;
        public ushort Icon;
        public bool CancelEffect;
        public bool RenewEffect;
        public eEffect EffectType;
        public GameLiving Owner;
        public int TickInterval;
        public long NextTick;

        public ECSGameEffect() { }

        public ECSGameEffect(GameLiving owner,ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
        {
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            //ExpireTick = 0;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            Icon = icon;
            CancelEffect = cancelEffect;
            RenewEffect = false;
            EffectType = MapEffect();
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            if (handler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (duration >> 1) + (int)StartTick;
            }
            else if (handler.Spell.SpellType == (byte)eSpellType.HealOverTime)
            {
                NextTick = StartTick;
            }
            else if (handler.Spell.SpellType == (byte)eSpellType.Confusion)
            {
                PulseFreq = 5000;
            }
        }

        public ushort GetRemainingTimeForClient()
        {
            if (Duration > 0)
                return (ushort)(ExpireTick - GameLoop.GameLoopTime);
            else
                return 0;
        }

        protected eEffect MapEffect()
        {
            Console.WriteLine("Spell of type: " + ((eSpellType)SpellHandler.Spell.SpellType).ToString());

            switch (SpellHandler.Spell.SpellType)
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
                    if (SpellHandler.SpellLine.IsBaseLine)
                        return eEffect.BaseAFBuff; //currently no map to specAF. where is spec AF handled?
                    else
                        return eEffect.SpecAFBuff;


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
                case (byte)eSpellType.DamageSpeedDecrease:
                case (byte)eSpellType.StyleSpeedDecrease:
                case (byte)eSpellType.SpeedDecrease:
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
                    if (SpellHandler.Spell.DamageType == eDamageType.Body)
                        return eEffect.BodyResistDebuff;
                    else if (SpellHandler.Spell.DamageType == eDamageType.Cold)
                        return eEffect.ColdResistDebuff;
                    else
                        return eEffect.Unknown;


                #endregion

                default:
                    Console.WriteLine($"Unable to map effect for ECSGameEffect! {this}");
                    return eEffect.Unknown;
            }
        }

    }
}