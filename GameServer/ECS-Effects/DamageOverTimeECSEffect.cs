using DOL.GS.Spells;

namespace DOL.GS
{
    public class DamageOverTimeECSGameEffect : ECSGameSpellEffect
    {
        public DamageOverTimeECSGameEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            // Tick here if the effect hasn't ticked yet.
            // This allows two poisons to do damage when being applied during the same server tick.
            // Otherwise, only one will call `OnEffectPulse`.
            if (SpellHandler is not DoTSpellHandler dotHandler || !dotHandler.FirstTick)
                return;

            OnEffectPulse();
            NextTick += PulseFreq;
        }

        public override void OnStopEffect()
        {
            // "Your mental agony fades."
            // "{0}'s mental agony fades."
            OnEffectExpiresMsg(true, false, true);
        }

        public override void OnEffectPulse()
        {
            if (!Owner.IsAlive)
                Stop();

            if (SpellHandler is not DoTSpellHandler dotHandler)
                return;

            // "Searing pain fills your mind!"
            // "{0} is wracked with pain!"
            if (OwnerPlayer != null)
                OnEffectStartsMsg(true, false, true);

            dotHandler.OnDirectEffect(Owner);
            FinalizeEffectPulse();
        }

        protected void FinalizeEffectPulse()
        {
            // DoTs subsequent ticks set `AttackData.CausesCombat` to false, but we need them to keep the victim (and only the victim) in combat.
            // We however respect the `IsAlive` check.
            if (!SpellHandler.Caster.IsAlive)
                return;

            if (Owner.Realm == 0 || SpellHandler.Caster.Realm == 0)
                Owner.LastAttackTickPvE = GameLoop.GameLoopTime;
            else
                Owner.LastAttackTickPvP = GameLoop.GameLoopTime;

            // NPCs interrupt with every DoT tick.
            if (SpellHandler.Caster is GameNPC)
                Owner.StartInterruptTimer(SpellHandler.Caster.SpellInterruptDuration, AttackData.eAttackType.Spell, SpellHandler.Caster);
        }
    }
}
