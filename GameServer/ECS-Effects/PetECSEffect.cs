using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class PetECSGameEffect : ECSGameSpellEffect
    {
        public PetECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStopEffect()
        {
            if (SpellHandler.Caster.PetCount > 0)
                SpellHandler.Caster.PetCount--;
            Owner.Health = 0; // to send proper remove packet
            Owner.Delete();
        }
    }
}
