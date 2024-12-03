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

        public ECSPulseEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base (new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            PulseFreq = pulseFreq;
            CancelEffect = cancelEffect;
            EffectType = eEffect.Pulse;
            StartTick = GameLoop.GameLoopTime;
            NextTick = pulseFreq + GameLoop.GameLoopTime;
            EffectService.RequestStartEffect(this);
        }

        public override void OnStartEffect()
        {
            Spell spell = SpellHandler.Spell;
            Owner.ActivePulseSpells.AddOrUpdate(spell.SpellType, spell, (x, y) => spell);
        }

        public override void OnStopEffect()
        {
            Owner.ActivePulseSpells.TryRemove(SpellHandler.Spell.SpellType, out _);
        }
    }
}
