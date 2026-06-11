namespace DOL.GS.Effects
{
    public class BunkerOfFaithECSEffect : StatBuffECSEffect
    {
        public override string Name => "Bunker of Faith";
        public override bool HasPositiveEffect => true;

        public BunkerOfFaithECSEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }
    }
}
