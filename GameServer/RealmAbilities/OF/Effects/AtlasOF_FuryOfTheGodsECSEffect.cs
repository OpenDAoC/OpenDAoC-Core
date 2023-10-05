namespace DOL.GS.Effects
{
    public class AtlasOF_FuryOfTheGodsECSEffect : DamageAddEcsSpellEffect
    {
        public AtlasOF_FuryOfTheGodsECSEffect(EcsGameEffectInitParams initParams)
            : base(initParams)
        {
        }
        
        public override ushort Icon { get { return 4251; } }
        public override string Name { get { return "Fury Of The Gods"; } }
        public override bool HasPositiveEffect { get { return true; } }
    }
}
