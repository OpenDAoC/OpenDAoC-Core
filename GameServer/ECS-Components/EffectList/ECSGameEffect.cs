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
                case "StrengthBuff":
                    return eEffect.BaseStr;
                case "DexterityBuff":
                    return eEffect.BaseDex;
                case "ConstitutionBuff":
                    return eEffect.BaseCon;
                case "StrengthConstitutionBuff":
                    return eEffect.StrCon;
                case "DexterityQuicknessBuff":
                    return eEffect.DexQui;
                case "AcuityBuff":
                    return eEffect.Acuity;
                case "ArmorAbsorptionBuff":
                    return eEffect.ArmorAbsorptionBuff;
                case "ArmorFactorBuff":
                    return eEffect.BaseAf; //currently no map to specAF. where is spec AF handled?

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

                default:
                    Console.WriteLine($"Unable to map effect for ECSGameEffect! {this}");
                    return eEffect.Unknown;
            }
        }

    }
}