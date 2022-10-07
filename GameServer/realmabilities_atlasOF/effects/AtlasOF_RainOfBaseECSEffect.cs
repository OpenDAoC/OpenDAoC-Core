namespace DOL.GS.Effects
{
    public class AtlasOF_RainOfBaseECSEffect : DamageAddECSEffect
    {
        public AtlasOF_RainOfBaseECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override bool HasPositiveEffect => true;
    }
}
