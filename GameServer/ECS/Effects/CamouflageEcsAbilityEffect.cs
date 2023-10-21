using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.ECS;

public class CamouflageEcsAbilityEffect : EcsGameAbilityEffect
{
    public CamouflageEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.Camouflage;
        EffectService.RequestStartEffect(this);
    }

    public override ushort Icon { get { return 476; } }
    public override string Name { get { return LanguageMgr.GetTranslation(((GamePlayer)Owner).Client, "Effects.CamouflageEffect.Name"); } }
    public override bool HasPositiveEffect { get { return true; } }

    public override void OnStartEffect()
    {
        if (OwnerPlayer != null)
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.CamouflageEffect.YouAreCamouflaged"), EChatType.CT_Spell, EChatLoc.CL_SystemWindow);
    }
    public override void OnStopEffect()
    {
        if (OwnerPlayer != null)
            OwnerPlayer.Out.SendMessage(LanguageMgr.GetTranslation(OwnerPlayer.Client, "Effects.CamouflageEffect.YourCFIsGone"), EChatType.CT_System, EChatLoc.CL_SystemWindow);
    }
}