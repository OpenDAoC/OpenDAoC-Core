using DOL.GS.Spells;

namespace DOL.GS
{
    public class StatBuffECSEffect : ECSGameSpellEffect
    {
        public StatBuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            base.OnStartEffect();

            if (OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges++;

            if (EffectType is eEffect.MovementSpeedBuff)
            {
                if (!Owner.IsStealthed)
                {
                    Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, EffectType, SpellHandler.Spell.Value / 100.0);
                    Owner.OnMaxSpeedChange();
                }

                if (Owner.IsStealthed)
                    EffectService.RequestDisableEffect(this);
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, false);
            }

            // Let's not bother checking the effect type and simply attempt to start every regeneration timer instead.
            Owner.StartHealthRegeneration();
            Owner.StartEnduranceRegeneration();
            Owner.StartPowerRegeneration();

            // "You feel more dexterous!"
            // "{0} looks more agile!"
            OnEffectStartsMsg(Owner, true, true, true);
        }

        public override void OnStopEffect()
        {
            base.OnStopEffect();

            if (OwnerPlayer != null && OwnerPlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges--;

            if (EffectType is eEffect.MovementSpeedBuff)
            {
                Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, EffectType);
                Owner.OnMaxSpeedChange();
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, true);
            }

            // "Your agility returns to normal."
            // "{0} loses their graceful edge.""
            OnEffectExpiresMsg(Owner, true, false, true);
            IsBuffActive = false;
        }
    }
}
