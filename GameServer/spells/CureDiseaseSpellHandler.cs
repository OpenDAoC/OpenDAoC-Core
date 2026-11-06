using System;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureDisease)]
    public class CureDiseaseSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => $"All disease effects are removed from the target{GetFrequencyAndDurationSuffix()}.";

        private static readonly eEffect[] _effects = [eEffect.Disease];

        protected override ReadOnlySpan<eEffect> EffectsToRemove => _effects;

        public CureDiseaseSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
