namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.CureNearsightCustom)]
    public class CureNearsightCustomSpellHandler : CureNearsightSpellHandler
    {
        private Spell _spell;

        public override string ShortDescription => $"All nearsight effects are removed from the target. This spell's cast time is not influenced by stats{GetFrequencyAndDurationSuffix()}.";

        public CureNearsightCustomSpellHandler(GameLiving caster, Spell spell, SpellLine line): base(caster, spell, line)
        {
            _spell = spell;
        }

        public override int CalculateCastingTime()
        {
            return _spell.CastTime;
        }
    }
}
