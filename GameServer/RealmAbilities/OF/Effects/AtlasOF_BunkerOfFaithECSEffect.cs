namespace DOL.GS.Effects
{
    public class BunkerOfFaithECSEffect : StatBuffEcsSpellEffect
    {
        public override ushort Icon => 4242;
        public override string Name => "Bunker of Faith";
        public override bool HasPositiveEffect => true;

        public BunkerOfFaithECSEffect(EcsGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.ArmorAbsorptionBuff;
        }
    }
}
