using Core.GS.Enums;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.MasterLevels;

//shared timer 5
#region Warlord-10
[SpellHandler("MLABSBuff")]
public class WarguardSpell : MasterLevelBuffHandling
{
    public override EProperty Property1 { get { return EProperty.ArmorAbsorption; } }

    public WarguardSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line) { }
}
#endregion