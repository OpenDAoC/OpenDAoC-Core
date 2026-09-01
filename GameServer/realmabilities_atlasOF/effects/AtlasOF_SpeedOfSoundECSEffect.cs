namespace DOL.GS.Effects
{
    public class SpeedOfSoundECSEffect : ECSGameAbilityEffect
    {
        public SpeedOfSoundECSEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.SpeedOfSound;
        }

        public override ushort Icon => 4249;

        public override string Name => "Speed Of Sound";

        public override bool HasPositiveEffect => true;

        public override void OnStartEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }
    }
}
