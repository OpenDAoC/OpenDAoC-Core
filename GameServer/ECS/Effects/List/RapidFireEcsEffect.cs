﻿using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class RapidFireEcsEffect : EcsGameAbilityEffect
    {
        public RapidFireEcsEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.RapidFire;
            EffectService.RequestStartEffect(this);
        }


        public override ushort Icon { get { return 484; } }
        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.RapidFireEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.RapidFireEffect.YouSwitchRFMode"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
        }
        public override void OnStopEffect()
        {

        }
    }
}