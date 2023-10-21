using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //shared timer 3
    
    [SpellHandler("Critical")]
    public class GuidedStrikeSpell : MasterlevelDualBuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.CriticalSpellHitChance; }
        }

        public override EProperty Property2
        {
            get { return EProperty.CriticalMeleeHitChance; }
        }

        public GuidedStrikeSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}