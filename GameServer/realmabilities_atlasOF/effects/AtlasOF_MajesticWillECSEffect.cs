using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_MajesticWillECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_MajesticWillECSEffect(in ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.MajesticWill;
            Start();
        }

        public override ushort Icon { get { return 4239; } }
        public override string Name { get { return "Majestic Will"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
