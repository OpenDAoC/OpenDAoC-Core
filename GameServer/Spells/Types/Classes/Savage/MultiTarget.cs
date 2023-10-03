namespace DOL.GS.Spells
{
    [SpellHandler("MultiTarget")]
    public class MultiTargetSpellHandler : SpellHandler
    {
        public MultiTargetSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}
