
namespace DOL.GS.Effects
{
    public class MasteryOfConcentrationECSEffect : ECSGameAbilityEffect
    {
        public MasteryOfConcentrationECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.MasteryOfConcentration;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 3006; } }
        public override string Name { get { return "Mastery of Concentration"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
