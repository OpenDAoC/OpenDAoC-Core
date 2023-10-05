using System;

namespace DOL.GS.Effects
{
    public class AtlasOF_WhirlingDervishECSEffect : EcsGameAbilityEffect
    {
        public AtlasOF_WhirlingDervishECSEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.WhirlingDervish;
            EffectService.RequestStartEffect(this);
        }
        
        public override ushort Icon { get { return 4282; } }
        public override string Name { get { return "Whirling Dervish"; } }
        
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            Owner.AbilityBonus[eProperty.OffhandDamageAndChance] += 5 * (int)Math.Round(this.Effectiveness);
            base.OnStartEffect();
        }
        
        public override void OnStopEffect()
        {
            Owner.AbilityBonus[eProperty.OffhandDamageAndChance] -= 5 * (int)Math.Round(this.Effectiveness);
            base.OnStopEffect();
        }
    }
}
