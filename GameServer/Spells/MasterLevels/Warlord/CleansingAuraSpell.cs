namespace Core.GS.Spells
{
    //shared timer 3

    [SpellHandler("CleansingAura")]
    public class CleansingAuraSpell : SpellHandler
    {
        public override bool IsOverwritable(EcsGameSpellEffect compare)
        {
            return true;
        }

        public CleansingAuraSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}