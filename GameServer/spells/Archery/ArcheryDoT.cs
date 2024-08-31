namespace DOL.GS.Spells
{
    [SpellHandler("ArcheryDoT")]
    public class ArcheryDoTSpellHandler : DoTSpellHandler
    {
        public ArcheryDoTSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
    }
}
