using System.Collections.Generic;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Expansions.TrialsOfAtlantis.Spells.MasterLevels;

[SpellHandler("BodyguardHandler")]
public class BodyguardSpell : SpellHandler
{
    public override bool CheckBeginCast(GameLiving selectedTarget)
    {
        //    if (Caster.Group.MemberCount <= 2)
        //    {
        //        MessageToCaster("Your group is to small to use this spell.", eChatType.CT_Important);
        //        return false;
        //    }
        return base.CheckBeginCast(selectedTarget);

    }

    public override IList<string> DelveInfo
    {
        get
        {
            var list = new List<string>();
            list.Add(Spell.Description);
            return list;
        }
    }

    public BodyguardSpell(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
    {
    }
}