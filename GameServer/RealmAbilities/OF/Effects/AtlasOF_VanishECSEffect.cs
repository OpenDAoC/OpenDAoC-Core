using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_VanishECSEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public AtlasOF_VanishECSEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.Vanish;
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
