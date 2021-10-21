using DOL.GS.Spells;
using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class BladeturnECSGameEffect : ECSGameSpellEffect
    {
        public BladeturnECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            eChatType toLiving = (SpellHandler.Spell.Pulse == 0) ? eChatType.CT_Spell : eChatType.CT_SpellPulse;
            eChatType toOther = (SpellHandler.Spell.Pulse == 0) ? eChatType.CT_System : eChatType.CT_SpellPulse;
            (SpellHandler as BladeturnSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message1, toLiving);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message2, Owner.GetName(0, false)), toOther, Owner);
        }

        public override void OnStopEffect()
        {
            (SpellHandler as BladeturnSpellHandler).MessageToLiving(Owner, SpellHandler.Spell.Message3, eChatType.CT_SpellExpires);
            Message.SystemToArea(Owner, Util.MakeSentence(SpellHandler.Spell.Message4, Owner.GetName(0, false)), eChatType.CT_SpellExpires, Owner);
        }
    }
}