
namespace DOL.GS.Effects
{
    // It looks like the real name should be "Strike the Soul", but renaming it requires updating the database.
    public class StrikingTheSoulECSEffect : ECSGameAbilityEffect
    {
        public StrikingTheSoulECSEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.StrikingTheSoul;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon => 4271;
        public override string Name => "Striking the Soul";
        public override bool HasPositiveEffect => true;
        private NecromancerPet _necromancerPet;

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            _necromancerPet = OwnerPlayer.ControlledBrain?.Body as NecromancerPet;

            // This implies that using the RA before summoning a pet will not make it receive the to-hit bonus.
            if (_necromancerPet == null)
                return;

            _necromancerPet.BuffBonusCategory4[(int)eProperty.ToHitBonus] += (int)Effectiveness;
            base.OnStartEffect();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null || _necromancerPet == null)
                return;

            _necromancerPet.BuffBonusCategory4[(int)eProperty.ToHitBonus] -= (int)Effectiveness;
            base.OnStopEffect();
        }
    }
}
