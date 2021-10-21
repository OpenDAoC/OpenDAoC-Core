using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class PiercingMagicECSGameEffect : ECSGameSpellEffect
    {
        public PiercingMagicECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            Owner.Effectiveness += (SpellHandler.Spell.Value / 100);
        }

        public override void OnStopEffect()
        {
             Owner.Effectiveness -= (SpellHandler.Spell.Value / 100);
        }
    }
}
