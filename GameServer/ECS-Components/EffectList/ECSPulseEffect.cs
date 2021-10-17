using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSPulseEffect : ECSGameEffect
    {
        /// <summary>
		/// The name of the owner
		/// </summary>
		public override string OwnerName
        {
            get { return "Pulse: " + SpellHandler.Spell.Name; }
        }

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base ()
        {
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            CancelEffect = cancelEffect;
            EffectType = eEffect.Pulse;//MapEffect();
            ExpireTick = pulseFreq + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;
        }
    }
}