using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameEffect
    {

        public ECSImmunityEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base()
        {
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            Icon = icon;
            CancelEffect = cancelEffect;
            EffectType = MapImmunityEffect();
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;
        }

        protected eEffect MapImmunityEffect()
        {
            switch (SpellHandler.Spell.SpellType)
            {
                case (byte)eSpellType.Mesmerize:
                    return eEffect.MezImmunity;
                case (byte)eSpellType.Stun:
                    return eEffect.StunImmunity;

                default:
                    return eEffect.Unknown;
            }
        }
    }
}