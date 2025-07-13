namespace DOL.GS.Effects
{
    public class AtlasOF_RainOfBaseECSEffect : DamageAddECSEffect
    {
        public AtlasOF_RainOfBaseECSEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override bool HasPositiveEffect => true;
    }
}
