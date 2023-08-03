using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSPulseEffect : ECSGameSpellEffect, IConcentrationEffect
    {
        /// <summary>
        /// The name of the owner
        /// </summary>
        public override string OwnerName => $"Pulse: {SpellHandler.Spell.Name}";

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base (new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            PulseFreq = pulseFreq;
            CancelEffect = cancelEffect;
            EffectType = eEffect.Pulse;
            ExpireTick = pulseFreq + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            EffectService.RequestStartEffect(this);
        }
    }
}
