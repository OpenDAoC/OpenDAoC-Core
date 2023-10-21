using Core.GS.ECS;

namespace Core.GS.Effects
{
    public class OfRaRainOfBaseEcsEffect : DamageAddEcsSpellEffect
    {
        public OfRaRainOfBaseEcsEffect(EcsGameEffectInitParams initParams) : base(initParams) { }

        public override bool HasPositiveEffect => true;
    }
}
