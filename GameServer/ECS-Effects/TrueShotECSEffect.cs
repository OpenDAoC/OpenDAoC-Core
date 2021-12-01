using DOL.GS.PacketHandler;
using DOL.Language;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DOL.GS
{
    public class TrueShotECSGameEffect : ECSGameAbilityEffect
    {
        public TrueShotECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.TrueShot;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 3004; } }
        public override string Name { get { return "Trueshot"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer != null)
            {
                OwnerPlayer.Out.SendMessage("You prepare a Trueshot!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }
        public override void OnStopEffect()
        {

        }
    }
}
