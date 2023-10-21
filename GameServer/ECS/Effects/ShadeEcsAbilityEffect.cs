using Core.GS.Enums;
using Core.Language;

namespace Core.GS.ECS;

public class ShadeEcsAbilityEffect : EcsGameAbilityEffect
{
    public ShadeEcsAbilityEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.Shade;
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