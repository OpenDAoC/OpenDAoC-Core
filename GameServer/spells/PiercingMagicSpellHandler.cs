using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS.Spells
{
    [SpellHandler("PiercingMagic")]
    public class PiercingMagicSpellHandler : SpellHandler
    {


        // constructor
        public PiercingMagicSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }   
}
