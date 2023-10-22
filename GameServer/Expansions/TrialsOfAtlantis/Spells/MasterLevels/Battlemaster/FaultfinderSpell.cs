using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

[SpellHandler("KeepDamageBuff")]
public class FaultfinderSpell : MasterLevelBuffHandling
{
    public override EProperty Property1
    {
        get { return EProperty.KeepDamage; }
    }

    public FaultfinderSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}