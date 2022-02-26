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

            // "A crystal shield covers you."
            // "A crystal shield covers {0}'s skin."
            OnEffectStartsMsg(Owner, true, false, true);
        }

        public override void OnStopEffect()
        {
            Owner.TempProperties.removeProperty(AblativeArmorSpellHandler.ABLATIVE_HP);
            // "Your crystal shield fades."
            // "{0}'s crystal shield fades."
            OnEffectExpiresMsg(Owner, true, false, true);

            //}
        }
    }
}
