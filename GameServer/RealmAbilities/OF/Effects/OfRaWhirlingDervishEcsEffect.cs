using System;

namespace DOL.GS.Effects
{
    public class OfRaWhirlingDervishEcsEffect : EcsGameAbilityEffect
    {
        public OfRaWhirlingDervishEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.WhirlingDervish;
            EffectService.RequestStartEffect(this);
        }
        
        public override ushort Icon { get { return 4282; } }
        public override string Name { get { return "Whirling Dervish"; } }
        
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            Owner.AbilityBonus[EProperty.OffhandDamageAndChance] += 5 * (int)Math.Round(this.Effectiveness);
            base.OnStartEffect();
        }
        
        public override void OnStopEffect()
        {
            Owner.AbilityBonus[EProperty.OffhandDamageAndChance] -= 5 * (int)Math.Round(this.Effectiveness);
            base.OnStopEffect();
        }
    }
}
