using System.Collections.Generic;
using Core.GS.Skills;

namespace Core.GS.Spells
{
    //no shared timer

    [SpellHandler("SickHeal")]
    public class SoulRestorationSpell : RemoveSpellEffectHandler
    {
        // constructor
        public SoulRestorationSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            m_spellTypesToRemove = new List<string>();
            m_spellTypesToRemove.Add("PveResurrectionIllness");
            m_spellTypesToRemove.Add("RvrResurrectionIllness");
        }
    }
}