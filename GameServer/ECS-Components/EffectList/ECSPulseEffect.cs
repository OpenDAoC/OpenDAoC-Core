using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSPulseEffect : ECSGameEffect
    {

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base ()
        {
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            Icon = 0;
            CancelEffect = cancelEffect;
            EffectType = eEffect.Pulse;//MapEffect();
            ExpireTick = pulseFreq + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;
        }
    }
}