using DOL.GS.PacketHandler;

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
            
            // "A crystal shield covers you."
            // "A crystal shield covers {0}'s skin."
            OnEffectStartsMsg(true, false, true);

            //GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
        }

        public override void OnStopEffect()
        {
            // "Your crystal shield fades."
            // "{0}'s crystal shield fades."
            OnEffectExpiresMsg(true, false, true);

        }
    }
}
