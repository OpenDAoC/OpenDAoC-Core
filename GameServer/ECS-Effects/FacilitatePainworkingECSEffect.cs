using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class FacilitatePainworkingECSGameEffect : ECSGameSpellEffect
    {
        public FacilitatePainworkingECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            Owner.InterruptTime = 0;
            Owner.InterruptAction = 0;
        }

        public override void OnStopEffect()
        {

        }
    }
}
