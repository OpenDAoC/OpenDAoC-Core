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

            foreach (ECSGameSpellEffect speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff))
            {
                if (speedBuff.GetType() != typeof(SpeedOfSoundECSEffect))
                    speedBuff.Disable();
            }

            foreach (ECSGameSpellEffect snare in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.Snare))
                snare.Disable();

            foreach (ECSGameSpellEffect root in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedDebuff))
                root.Disable();

            foreach (ECSGameSpellEffect ichor in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.Ichor))
                ichor.Disable();

            OwnerPlayer.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this, PropertyCalc.MaxSpeedCalculator.SPEED4);
            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }

        public override void OnStopEffect()
        {
            if (OwnerPlayer == null)
                return;

            OwnerPlayer.BuffBonusMultCategory1.Remove((int)eProperty.MaxSpeed, this);

            foreach (ECSGameSpellEffect speedBuff in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedBuff))
                speedBuff.Enable();

            foreach (ECSGameSpellEffect snare in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.Snare))
                snare.Enable();

            foreach (ECSGameSpellEffect root in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.MovementSpeedDebuff))
                root.Enable();

            foreach (ECSGameSpellEffect ichor in OwnerPlayer.effectListComponent.GetSpellEffects(eEffect.Ichor))
                ichor.Enable();

            OwnerPlayer.Out.SendUpdateMaxSpeed();
        }
    }
}
