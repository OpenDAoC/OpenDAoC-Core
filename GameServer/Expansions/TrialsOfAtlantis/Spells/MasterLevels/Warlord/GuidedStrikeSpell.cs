using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

//shared timer 3

[SpellHandler("Critical")]
public class GuidedStrikeSpell : MasterLevelDualBuffHandling
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