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
            switch (SpellHandler.Spell.SpellType)
            {
                case "ConstitutionBuff":
                    return eEffect.BaseCon;
                default:
                    Console.WriteLine($"Unable to map effect for ECSGameEffect! {this}");
                    return eEffect.Unknown;
            }
        }
    }
}