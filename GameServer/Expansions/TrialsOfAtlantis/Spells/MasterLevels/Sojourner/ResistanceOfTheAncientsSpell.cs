using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

//no shared timer

[SpellHandler("EssenceResist")]
public class ResistanceOfTheAncientsSpell : AResistBuff
{
    public override EBuffBonusCategory BonusCategory1
    {
        get { return EBuffBonusCategory.BaseBuff; }
    }

    public override EProperty Property1
    {
        get { return EProperty.Resist_Natural; }
    }

    public ResistanceOfTheAncientsSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}