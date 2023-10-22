using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

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