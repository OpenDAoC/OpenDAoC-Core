namespace DOL.GS.Effects
{
    public class AtlasOF_RainOfBaseECSEffect : DamageAddEcsSpellEffect
    {
        public AtlasOF_RainOfBaseECSEffect(EcsGameEffectInitParams initParams) : base(initParams) { }

        public override bool HasPositiveEffect => true;
    }
}
