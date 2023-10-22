using Core.GS.ECS;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

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