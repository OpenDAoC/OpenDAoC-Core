using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS
{
    public class ShadeECSGameEffect : ECSGameAbilityEffect
    {
        public override ushort Icon => 0x193;
        public override string Name => LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.ShadeEffect.Name");
        public override bool HasPositiveEffect => false;

        public ShadeECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.Shade;
        }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            if (OwnerPlayer.HasShadeModel)
                return;

            OwnerPlayer.Shade(true);
            OwnerPlayer.Model = OwnerPlayer.ShadeModel;
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            if (!OwnerPlayer.HasShadeModel)
                return;

            OwnerPlayer.Shade(false);
            OwnerPlayer.Model = OwnerPlayer.CreationModel;
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client.Account.Language, "GamePlayer.Shade.NoLongerShade"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
