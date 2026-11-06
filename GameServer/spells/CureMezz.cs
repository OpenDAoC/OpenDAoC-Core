using System;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureMezz)]
    public class CureMezzSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => $"All mesmerization effects are removed from the target{GetFrequencyAndDurationSuffix()}.";

        private static readonly eEffect[] _effects = [eEffect.Mez];

        protected override ReadOnlySpan<eEffect> EffectsToRemove => _effects;

        public CureMezzSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
