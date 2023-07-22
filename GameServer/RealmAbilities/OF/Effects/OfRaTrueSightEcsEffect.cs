using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class OfRaTrueSightEcsEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public OfRaTrueSightEcsEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.TrueSight;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4279; } }
        public override string Name { get { return "True Sight"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
