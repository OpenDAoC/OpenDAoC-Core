using DOL.GS.Spells;
using DOL.GS.Effects;

namespace DOL.GS
{
    public class ECSPulseEffect : ECSGameSpellEffect, IConcentrationEffect
    {
        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;

        /// <summary>
        /// The name of the owner
        /// </summary>
        public override string OwnerName
        {
            get { return "Pulse: " + SpellHandler.Spell.Name; }
        }

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base (new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            // Some of this is already done in the base constructor and should be cleaned up
            //Owner = owner;
            //SpellHandler = handler;
            //Duration = duration;
            PulseFreq = pulseFreq;
            //Effectiveness = effectiveness;
            CancelEffect = cancelEffect;
            EffectType = eEffect.Pulse;
            ExpireTick = pulseFreq + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            EntityManager.AddEffect(this);
        }
    }
}