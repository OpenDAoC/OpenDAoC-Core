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
        public override void CreateECSEffect(ECSGameEffectInitParams initParams)
        {
            new PiercingMagicECSGameEffect(initParams);
        }
        // constructor
        public PiercingMagicSpellHandler(GameLiving caster, Spell spell, SpellLine line) : base(caster, spell, line)
        {
        }
    }   
}
