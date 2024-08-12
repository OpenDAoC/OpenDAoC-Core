using DOL.GS.Spells;

namespace DOL.GS
{
    public class DamageOverTimeECSGameEffect : ECSGameSpellEffect
    {
        public DamageOverTimeECSGameEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            NextTick = GameLoop.GameLoopTime;
        }

        public override void OnStartEffect()
        {
            // Remove stealth on first application.
            if (OwnerPlayer != null && !OwnerPlayer.effectListComponent.ContainsEffectForEffectType(eEffect.Vanish))
                OwnerPlayer.Stealth(false);
        }

        public override void OnStopEffect()
        {
            // "Your mental agony fades."
            // "{0}'s mental agony fades."
            OnEffectExpiresMsg(Owner, true, false, true);
        }

        public override void OnEffectPulse()
        {
            if (!Owner.IsAlive)
                EffectService.RequestImmediateCancelEffect(this);

            if (SpellHandler is not DoTSpellHandler dotHandler)
                return;

            if (OwnerPlayer != null)
            {
                // "Searing pain fills your mind!"
                // "{0} is wracked with pain!"
                OnEffectStartsMsg(Owner, true, false, true);
            }

            if (dotHandler.Caster.effectListComponent.ContainsEffectForEffectType(eEffect.Viper) && dotHandler.Spell.IsPoison)
            {
                Effectiveness *= 2;
                dotHandler.OnDirectEffect(Owner);
                Effectiveness /= 2;
            }
            else
                dotHandler.OnDirectEffect(Owner);

            FinalizeEffectPulse();
        }

        protected void FinalizeEffectPulse()
        {
            if (Owner.Realm == 0 || SpellHandler.Caster.Realm == 0)
                Owner.LastAttackTickPvE = GameLoop.GameLoopTime;
            else
                Owner.LastAttackTickPvP = GameLoop.GameLoopTime;

            if (SpellHandler.Caster is GameSummonedPet)
                Owner.StartInterruptTimer(SpellHandler.Caster.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
        }
    }
}
