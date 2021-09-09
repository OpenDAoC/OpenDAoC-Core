using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    /// <summary>
    /// Effect that stays on target and does additional
    /// damage after each melee attack
    /// </summary>
    [SpellHandler("StyleRange")]
    public class LongRangeSpellHandler : SpellHandler
    {
        public LongRangeSpellHandler(GameLiving caster, Spell spell, SpellLine spellLine) : base(caster, spell, spellLine)
        {
        }
    }
}
