using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class QuickCastECSGameEffect : ECSGameAbilityEffect
    {
        public QuickCastECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.QuickCast;
            EffectService.RequestStartEffect(this);
        }

        public const int DURATION = 3000;

        public override ushort Icon { get { return 0x0190; } }
        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.QuickCastEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }
        public override long GetRemainingTimeForClient() { { return 0; } }

        public override void OnStartEffect()
        {
            if (Owner is GamePlayer)
                (Owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((Owner as GamePlayer).Client, "Effects.QuickCastEffect.YouActivatedQC"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            Owner.TempProperties.removeProperty(Spells.SpellHandler.INTERRUPT_TIMEOUT_PROPERTY);
        }
        public override void OnStopEffect()
        {

        }
        public void Cancel(bool playerCancel)
        {
            if (playerCancel)
            {
                if (Owner is GamePlayer)
                    (Owner as GamePlayer).Out.SendMessage(LanguageMgr.GetTranslation((Owner as GamePlayer).Client, "Effects.QuickCastEffect.YourNextSpellNoQCed"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }

            EffectService.RequestImmediateCancelEffect(this, playerCancel);
        }
    }
}
