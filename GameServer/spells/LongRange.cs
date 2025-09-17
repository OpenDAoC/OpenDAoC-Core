namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.StyleRange)]
    public class LongRangeSpellHandler : SpellHandler
    {
        public override string ShortDescription => $"Hits target up to {Spell.Value} units away.";

        public LongRangeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
