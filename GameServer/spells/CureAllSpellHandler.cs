using System;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureAll)]
    public class CureAllSpellHandler : RemoveSpellEffectHandler
    {
        private static readonly eEffect[] _effects =
        [
            eEffect.DamageOverTime,
            eEffect.Nearsight,
            eEffect.Disease,
            eEffect.Bleed
        ];

        protected override ReadOnlySpan<eEffect> EffectsToRemove => _effects;

        public CureAllSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
