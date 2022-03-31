using DOL.GS.PacketHandler;

namespace DOL.GS.Effects
{
    public class AtlasOF_RainOfAnnihilationECSEffect : DamageAddECSEffect
    {
        public AtlasOF_RainOfAnnihilationECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        
        public override ushort Icon { get { return 4237; } }
        public override string Name { get { return "Rain Of Annihilation"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
