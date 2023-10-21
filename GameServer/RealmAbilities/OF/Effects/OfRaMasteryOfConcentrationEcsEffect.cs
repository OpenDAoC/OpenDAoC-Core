
namespace DOL.GS.Effects
{
    public class OfRaMasteryOfConcentrationEcsEffect : EcsGameAbilityEffect
    {
        public OfRaMasteryOfConcentrationEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.MasteryOfConcentration;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4238; } }
        public override string Name { get { return "Mastery of Concentration"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
