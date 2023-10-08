namespace DOL.GS.Effects
{
    public class AtlasOF_HailOfBlowsECSEffect : StatBuffEcsSpellEffect
    {
        public AtlasOF_HailOfBlowsECSEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
            EffectType = EEffect.MeleeHasteBuff;
        }
        
        public override ushort Icon { get { return 4240; } }
        public override string Name { get { return "Hail Of Blows"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
