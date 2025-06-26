using DOL.GS.RealmAbilities;

namespace DOL.GS.Effects
{
    public class AtlasOF_LongshotECSEffect : TrueShotECSGameEffect
    {
        public override ushort Icon => 4276;
        public override string Name => "Longshot";
        public override bool HasPositiveEffect => true;

        public AtlasOF_LongshotECSEffect(TimedRealmAbility ability, ECSGameEffectInitParams initParams) : base(ability, initParams) { }

        public override void OnStartEffect()
        {
            Owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Long;
            Owner.attackComponent.RequestStartAttack();
        }

        public override void OnStopEffect()
        {
            Owner.rangeAttackComponent.RangedAttackType = eRangedAttackType.Normal;
        }
    }
}
