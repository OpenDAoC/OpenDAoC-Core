using System;

namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CurePoison)]
    public class CurePoisonSpellHandler : RemoveSpellEffectHandler
    {
        public override string ShortDescription => $"All damage over time effects (such as poisons) are removed from the target{GetFrequencyAndDurationSuffix()}.";

        private static readonly eEffect[] _effects = [eEffect.DamageOverTime, eEffect.Bleed];

        protected override ReadOnlySpan<eEffect> EffectsToRemove => _effects;

        public CurePoisonSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
