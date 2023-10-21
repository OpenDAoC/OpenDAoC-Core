using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Spells;

namespace Core.GS.Effects
{
    public class OfRaVanishEcsEffect : EcsGameAbilityEffect
    {
        public new SpellHandler SpellHandler;
        public OfRaVanishEcsEffect(EcsGameEffectInitParams initParams)
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
