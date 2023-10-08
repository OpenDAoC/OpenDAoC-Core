﻿using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class SureShotEcsAbilityEffect : EcsGameAbilityEffect
    {
        public SureShotEcsAbilityEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.Berserk;
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
