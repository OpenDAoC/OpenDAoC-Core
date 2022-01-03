using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class CamouflageECSGameEffect : ECSGameAbilityEffect
    {
        public CamouflageECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Camouflage;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 476; } }
        public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.CamouflageEffect.Name"); } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.CamouflageEffect.YouAreCamouflaged"), eChatType.CT_Spell, eChatLoc.CL_SystemWindow);
        }
        public override void OnStopEffect()
        {
            if (OwnerPlayer != null)
                OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.CamouflageEffect.YourCFIsGone"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
