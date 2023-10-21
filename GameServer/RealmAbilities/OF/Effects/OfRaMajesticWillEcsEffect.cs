using Core.GS.Spells;

namespace Core.GS.Effects
{
    public class OfRaMajesticWillEcsEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public OfRaMajesticWillEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.MajesticWill;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4239; } }
        public override string Name { get { return "Majestic Will"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
