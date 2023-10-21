using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.RealmAbilities;

public class OfRaSoldiersBarricadeEcsEffect : EcsGameAbilityEffect
{
    public OfRaSoldiersBarricadeEcsEffect(EcsGameEffectInitParams initParams)
        : base(initParams)
    {
        EffectType = EEffect.SoldiersBarricade;
        EffectService.RequestStartEffect(this);
    }

    public override ushort Icon { get { return 4241; } }
    public override string Name { get { return "Soldier's Barricade"; } }
    public override bool HasPositiveEffect { get { return true; } }

    public override void OnStartEffect()
    {
        if (OwnerPlayer == null)
            return;

        OwnerPlayer.BuffBonusCategory4[(int)EProperty.ArmorFactor] += (int)Effectiveness;
        OwnerPlayer.Out.SendUpdateWeaponAndArmorStats();
    }

    public override void OnStopEffect()
    {
        if (OwnerPlayer == null)
            return;

        OwnerPlayer.BuffBonusCategory4[(int)EProperty.ArmorFactor] -= (int)Effectiveness;
        OwnerPlayer.Out.SendUpdateWeaponAndArmorStats();
    }
}