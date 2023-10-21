using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("StyleRange")]
    public class LongRangeSpell : SpellHandler
    {
        public LongRangeSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}
