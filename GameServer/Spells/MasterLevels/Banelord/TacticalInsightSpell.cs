using Core.GS.Enums;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //shared timer 5

    [SpellHandler("MissHit")]
    public class TacticalInsightSpell : MasterLevelBuffHandling
    {
        public override EProperty Property1
        {
            get { return EProperty.MissHit; }
        }

        // constructor
        public TacticalInsightSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }
}