
namespace DOL.GS.Effects
{
    public class SoldiersBarricadeECSEffect : ECSGameAbilityEffect
    {
        public SoldiersBarricadeECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.SoldiersBarricade;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4241; } }
        public override string Name { get { return "Soldier's Barricade"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusCategory4[(int)eProperty.ArmorFactor] += (int)Effectiveness;
            OwnerPlayer.Out.SendUpdateWeaponAndArmorStats();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusCategory4[(int)eProperty.ArmorFactor] -= (int)Effectiveness;
            OwnerPlayer.Out.SendUpdateWeaponAndArmorStats();
        }
    }
}
