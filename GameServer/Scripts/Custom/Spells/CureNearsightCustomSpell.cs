using System.Collections.Generic;
using Core.GS.Skills;
using Core.GS.Spells;

namespace Core.GS.Scripts
{
    /// <summary>
    /// 
    /// </summary>
    [SpellHandler("CureNearsightCustom")]
    public class CureNearsightCustomSpell : RemoveSpellEffectHandler
    {
        private Spell spell;
        // constructor
        public CureNearsightCustomSpell(GameLiving caster, Spell spell, SpellLine line)
            : base(caster, spell, line)
        {
            // RR4: now it's a list
            m_spellTypesToRemove = new List<string>();
            m_spellTypesToRemove.Add("Nearsight");
            m_spellTypesToRemove.Add("Silence");

            this.spell = spell;
        }

        public override int CalculateCastingTime()
        {
            return spell.CastTime;
        }
    }
}