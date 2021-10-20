using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class AblativeArmorECSGameEffect : ECSGameSpellEffect
    {
        public AblativeArmorECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            Owner.TempProperties.setProperty(AblativeArmorSpellHandler.ABLATIVE_HP, (int)SpellHandler.Spell.Value);
            //GameEventMgr.AddHandler(e.Owner, GameLivingEvent.AttackedByEnemy, new DOLEventHandler(OnAttack));

            eChatType toLiving = (SpellHandler.Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
            eChatType toOther = (SpellHandler.Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;
            (SpellHandler as AblativeArmorSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, toLiving);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, false)), toOther, Owner);
        }

        public override void OnStopEffect()
        {
            Owner.TempProperties.removeProperty(AblativeArmorSpellHandler.ABLATIVE_HP);
            //if (!noMessages && Spell.Pulse == 0)
            //{
            (SpellHandler as AblativeArmorSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, false)), eChatType.CT_SpellExpires, Owner);
            //}
        }
    }
}
