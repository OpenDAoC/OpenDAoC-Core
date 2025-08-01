namespace DOL.GS
{
    public class SpeedEnhancementECSEffect : ECSGameSpellEffect
    {
        public SpeedEnhancementECSEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override bool Enable()
        {
            return !Owner.IsStealthed && base.Enable();
        }

        public override void OnStartEffect()
        {
            base.OnStartEffect();
            Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this, SpellHandler.Spell.Value / 100.0);
            Owner.OnMaxSpeedChange();
            OnEffectStartsMsg(true, true, true);
        }

        public override void OnStopEffect()
        {
            base.OnStopEffect();
            Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, this);
            Owner.OnMaxSpeedChange();
        }

        public override bool FinalizeAddedState(EffectListComponent.AddEffectResult result)
        {
            // Movement speed buffs are always disabled when applied to a stealthed target.
            if (EffectType is eEffect.MovementSpeedBuff && result is EffectListComponent.AddEffectResult.Added && Owner.IsStealthed)
                return base.FinalizeAddedState(EffectListComponent.AddEffectResult.Disabled);
            else
                return base.FinalizeAddedState(result);
        }
    }
}
