using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class SureShotECSGameEffect : ECSGameAbilityEffect
    {
        public SureShotECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Berserk;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 485; } }
        public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer?.Client, "Effects.SureShotEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }
        public override long GetRemainingTimeForClient() { return 0; } 

        public override void OnStartEffect()
        {
            OwnerPlayer?.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer?.Client, "Effects.SureShotEffect.YouSwitchToSSMode"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
        public override void OnStopEffect()
        {

        }
    }
}
