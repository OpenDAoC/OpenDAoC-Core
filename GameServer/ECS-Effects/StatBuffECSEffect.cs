using DOL.GS.Spells;

namespace DOL.GS
{
    public class StatBuffECSEffect : ECSGameSpellEffect
    {
        public StatBuffECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            base.OnStartEffect();

            if (OwnerPlayer != null && GamePlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges++;

            if (EffectType is eEffect.MovementSpeedBuff)
            {
                if (!Owner.IsStealthed)
                {
                    Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, this, SpellHandler.Spell.Value / 100.0);
                    Owner.OnMaxSpeedChange();
                }

                if (Owner.IsStealthed)
                    Disable();
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, false);
            }

            // Let's not bother checking the effect type and simply attempt to start every regeneration timer instead.
            // This will also update health, endurance, and power if they're above their max amount.
            Owner.StartHealthRegeneration();
            Owner.StartEnduranceRegeneration();
            Owner.StartPowerRegeneration();

            // "You feel more dexterous!"
            // "{0} looks more agile!"
            OnEffectStartsMsg(true, true, true);
        }

        public override void OnStopEffect()
        {
            base.OnStopEffect();

            if (OwnerPlayer != null && GamePlayer.SelfBuffChargeIDs.Contains(SpellHandler.Spell.ID))
                OwnerPlayer.ActiveBuffCharges--;

            if (EffectType is eEffect.MovementSpeedBuff)
            {
                Owner.BuffBonusMultCategory1.Remove((int) eProperty.MaxSpeed, this);
                Owner.OnMaxSpeedChange();
            }
            else
            {
                if (SpellHandler is not PropertyChangingSpell propertyChangingSpell)
                    return;

                foreach (eProperty property in EffectService.GetPropertiesFromEffect(EffectType))
                    ApplyBonus(Owner, propertyChangingSpell.BonusCategory1, property, SpellHandler.Spell.Value, Effectiveness, true);
            }

            // Let's not bother checking the effect type and simply attempt to start every regeneration timer instead.
            // This will also update health, endurance, and power if they're above their max amount.
            Owner.StartHealthRegeneration();
            Owner.StartEnduranceRegeneration();
            Owner.StartPowerRegeneration();

            // "Your agility returns to normal."
            // "{0} loses their graceful edge.""
            OnEffectExpiresMsg(true, false, true);
        }
    }
}
