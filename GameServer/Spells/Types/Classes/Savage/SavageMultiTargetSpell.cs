using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("MultiTarget")]
    public class SavageMultiTargetSpell : SpellHandler
    {
        public SavageMultiTargetSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}
