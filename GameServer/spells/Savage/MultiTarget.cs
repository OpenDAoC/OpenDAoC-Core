namespace DOL.GS.Spells
{
    [SpellHandler(eSpellType.MultiTarget)]
    public class MultiTargetSpellHandler : SpellHandler
    {
        public override string ShortDescription => $"Hits {Spell.Value} additional target(s) within melee range.";

        public MultiTargetSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine) { }
    }
}
