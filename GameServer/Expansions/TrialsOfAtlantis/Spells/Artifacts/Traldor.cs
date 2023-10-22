using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.Artifacts;

[SpellHandler("Traldor")]
public class Traldor : DualStatBuff
{
    public Traldor(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }

    /// <summary>
    /// SpecBuffBonusCategory
    /// </summary>
	public override EBuffBonusCategory BonusCategory1 { get { return EBuffBonusCategory.SpecBuff; } }

    /// <summary>
    /// BaseBuffBonusCategory
    /// </summary>
	public override EBuffBonusCategory BonusCategory2 { get { return EBuffBonusCategory.BaseBuff; } }

    public override EProperty Property1
    {
        get { return EProperty.SpellDamage; }
    }

    public override EProperty Property2
    {
        get { return EProperty.ResistPierce; }
    }

}