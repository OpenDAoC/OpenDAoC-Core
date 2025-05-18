using DOL.GS.PacketHandler;

namespace DOL.GS
{
    public class TrueShotECSGameEffect : ECSGameAbilityEffect
    {
        public TrueShotECSGameEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.TrueShot;
            Start();
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
