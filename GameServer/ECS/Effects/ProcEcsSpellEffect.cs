using Core.GS.PacketHandler;

namespace Core.GS
{
    public class ProcEcsSpellEffect : EcsGameSpellEffect
    {
        public ProcEcsSpellEffect(EcsGameEffectInitParams initParams)
            : base(initParams) { }

        public override void OnStartEffect()
        {
            EChatType chatType = EChatType.CT_SpellPulse;
            if (SpellHandler.Spell.Pulse == 0)
            {
                chatType = EChatType.CT_Spell;
            }
            
            // "A crystal shield covers you."
            // "A crystal shield covers {0}'s skin."
            OnEffectStartsMsg(Owner, true, false, true);

            //GameEventMgr.AddHandler(effect.Owner, EventType, new DOLEventHandler(EventHandler));
        }

        public override void OnStopEffect()
        {
            // "Your crystal shield fades."
            // "{0}'s crystal shield fades."
            OnEffectExpiresMsg(Owner, true, false, true);

        }
    }
}
