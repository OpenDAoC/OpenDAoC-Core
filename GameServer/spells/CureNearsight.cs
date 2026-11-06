using System;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureNearsight)]
    public class CureNearsightSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => $"All nearsight effects are removed from the target{GetFrequencyAndDurationSuffix()}.";

        private static readonly eEffect[] _effects = [eEffect.Nearsight]; // Supposed to remove Silence too, once implemented.

        protected override ReadOnlySpan<eEffect> EffectsToRemove => _effects;

        public CureNearsightSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
