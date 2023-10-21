using Core.GS.Skills;

namespace Core.GS.Spells
{
    [SpellHandler("NaturesShield")]
    public class NaturesShieldSpell : SpellHandler
    {
        public NaturesShieldSpell(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}
