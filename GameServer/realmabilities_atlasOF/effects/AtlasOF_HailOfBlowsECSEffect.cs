using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS.Effects
{
    public class AtlasOF_HailOfBlowsECSEffect : StatBuffECSEffect
    {
        public AtlasOF_HailOfBlowsECSEffect(ECSGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = eEffect.MeleeHasteBuff;
        }
        
        public override ushort Icon { get { return 4240; } }
        public override string Name { get { return "Hail Of Blows"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
