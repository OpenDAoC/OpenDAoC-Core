using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class HealOverTimeECSGameEffect : ECSGameSpellEffect
    {
        public HealOverTimeECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) 
        {
            NextTick = StartTick;
        }

        public override void OnStartEffect()
        {
            // "You start healing faster."
            // "{0} starts healing faster."
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            //"Your meditative state fades."
            //"{0}'s meditative state fades."
            OnEffectExpiresMsg(Owner, true, false, true);
        }

        public override void OnEffectPulse()
        {
            (SpellHandler as HoTSpellHandler).OnDirectEffect(Owner, Effectiveness);
            NextTick += PulseFreq;
        }
    }
}
