namespace DOL.GS.Effects
{
    public class MasteryOfConcentrationECSEffect : ECSGameAbilityEffect
    {
        public MasteryOfConcentrationECSEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.MasteryOfConcentration;
        }

        public override ushort Icon { get { return 4238; } }
        public override string Name { get { return "Mastery of Concentration"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
