using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_MajesticWillECSEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_MajesticWillECSEffect(EcsGameEffectInitParams initParams)
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
