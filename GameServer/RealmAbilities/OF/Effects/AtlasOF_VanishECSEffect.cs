using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_VanishECSEffect : ECSGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_VanishECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.Vanish;
            EffectService.RequestStartEffect(this);
        }

        public override ushort Icon { get { return 4280; } }
        public override string Name { get { return "Vanish"; } }
        public override bool HasPositiveEffect { get { return true; } }

        public override void OnStartEffect()
        {
            OwnerPlayer.Stealth(true);
            base.OnStartEffect();
        }
    }
}
