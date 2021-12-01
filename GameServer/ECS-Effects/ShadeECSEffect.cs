using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class ShadeECSGameEffect : ECSGameAbilityEffect
    {
        public ShadeECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Shade;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 0x193; } }
        public override string Name { get { return LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.ShadeEffect.Name"); } }
        public override bool HasPositiveEffect { get { return false; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null)
            {
                OwnerPlayer.Out.SendUpdatePlayer();
            }
        }
        public override void OnStopEffect()
        {
            if (OwnerPlayer != null)
            {
                OwnerPlayer.Shade(false);
                OwnerPlayer.Out.SendUpdatePlayer();
            }
        }
    }
}
