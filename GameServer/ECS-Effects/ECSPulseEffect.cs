using System.Collections.Generic;
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
        public Dictionary<GameLiving, ECSGameSpellEffect> ChildEffects { get; } = [];

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base (new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            PulseFreq = pulseFreq;
            EffectType = eEffect.Pulse;
            StartTick = GameLoop.GameLoopTime;
            NextTick = pulseFreq + GameLoop.GameLoopTime;
            Start();
        }

        public override void OnStartEffect()
        {
            Spell spell = SpellHandler.Spell;
            Owner.ActivePulseSpells.AddOrUpdate(spell.SpellType, spell, (x, y) => spell);
        }

        public override void OnStopEffect()
        {
            Owner.ActivePulseSpells.TryRemove(SpellHandler.Spell.SpellType, out _);

            if (!SpellHandler.Spell.IsFocus)
                return;

            foreach (var pair in ChildEffects)
            {
                ECSGameSpellEffect effect = pair.Value;

                if (effect.EffectType is eEffect.FocusShield)
                    effect.Stop();
            }
        }
    }
}
