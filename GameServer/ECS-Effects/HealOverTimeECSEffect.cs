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
            (SpellHandler as HoTSpellHandler).SendEffectAnimation(Owner, 0, false, 1);
            //"{0} seems calm and healthy."
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, false)), eChatType.CT_Spell, Owner);
        }

        public override void OnStopEffect()
        {
            //"Your meditative state fades."
            (SpellHandler as HoTSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            //"{0}'s meditative state fades."
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, false)), eChatType.CT_SpellExpires, Owner);
        }

        public override void OnEffectPulse()
        {
            (SpellHandler as HoTSpellHandler).OnDirectEffect(Owner, Effectiveness);
            NextTick += PulseFreq;
        }
    }
}
