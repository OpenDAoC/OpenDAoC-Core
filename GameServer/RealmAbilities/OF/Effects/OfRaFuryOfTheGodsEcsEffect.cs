using Core.GS.ECS;

namespace Core.GS.Effects
{
    public class OfRaFuryOfTheGodsEcsEffect : DamageAddEcsSpellEffect
    {
        public OfRaFuryOfTheGodsEcsEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        
        public override ushort Icon { get { return 4251; } }
        public override string Name { get { return "Fury Of The Gods"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
