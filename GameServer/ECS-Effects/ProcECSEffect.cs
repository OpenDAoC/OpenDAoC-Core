using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class ProcECSGameEffect : ECSGameSpellEffect
    {
        public ProcECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            eChatType chatType = eChatType.CT_SpellPulse;
            if (SpellHandler.Spell.Pulse == 0)
            {
                chatType = eChatType.CT_Spell;
            }
            (SpellHandler as SpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, chatType);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, true)), chatType, Owner);
            //GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
        }

        public override void OnStopEffect()
        {
            (SpellHandler as SpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, true)), eChatType.CT_SpellExpires, Owner);
        }
    }
}
