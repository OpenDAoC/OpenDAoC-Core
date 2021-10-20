using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameSpellEffect
    {

        public ECSImmunityEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base(new ECSGameEffectInitParams(owner, duration, effectiveness))
        {
            // Some of this is already done in the base constructor and should be cleaned up
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            CancelEffect = cancelEffect;
            EffectType = MapImmunityEffect();
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            EntityManager.AddEffect(this);
        }

        protected eEffect MapImmunityEffect()
        {
            switch (SpellHandler.Spell.SpellType)
            {
                case (byte)eSpellType.Mesmerize:
                    return eEffect.MezImmunity;
                case (byte)eSpellType.StyleStun:
                case (byte)eSpellType.Stun:
                    return eEffect.StunImmunity;
                case (byte)eSpellType.SpeedDecrease:
                case (byte)eSpellType.DamageSpeedDecrease:
                    return eEffect.SnareImmunity;
                case (byte)eSpellType.Nearsight:
                    return eEffect.NearsightImmunity;
                default:
                    return eEffect.Unknown;
            }
        }
    }
}