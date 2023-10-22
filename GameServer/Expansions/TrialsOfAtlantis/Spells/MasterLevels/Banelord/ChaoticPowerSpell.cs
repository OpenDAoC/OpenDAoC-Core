using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

//shared timer 3

[SpellHandler("BLToHit")]
public class ChaoticPowerSpell : MasterLevelBuffHandling
{
    public override EProperty Property1
    {
        get { return EProperty.ToHitBonus; }
    }

    // constructor
    public ChaoticPowerSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}