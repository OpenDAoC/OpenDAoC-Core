using DOL.AI.Brain;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class DamageShieldECSEffect : ECSGameSpellEffect
    {
        private CombatCheckTimer _combatCheckTimer;

        public DamageShieldECSEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        public override void OnStartEffect()
        {
            // "Lashing energy ripples around you."
            // "Dangerous energy surrounds {0}."
            OnEffectStartsMsg(Owner, true, true, true);

            if (!SpellHandler.Spell.IsPulsing || Owner is not GameNPC npcOwner || npcOwner.Brain is not ControlledMobBrain npcBrain)
                return;

            // Try to retrieve the focus effect on the caster and create our timer if we find it.
            foreach (ECSPulseEffect pulseEffect in SpellHandler.Caster.effectListComponent.GetAllPulseEffects())
            {
                if (!pulseEffect.SpellHandler.Spell.IsFocus)
                    continue;

                _combatCheckTimer = new(npcBrain, pulseEffect);
                break;
            }
        }

        public override void OnStopEffect()
        {
            // "Your energy field dissipates."
            // "{0}'s energy field dissipates."
            OnEffectExpiresMsg(Owner, true, false, true);
            _combatCheckTimer?.Stop();
        }

        // Back in the days, if the currently selected target died, any focus effect would be canceled.
        // While this was arguably a bug, some players relied on it when killing NPCs one by one.
        // However this can also result in unvoluntary loss of focus.
        // This timer aims at doing something similar, but with more restrictive conditions.
        private class CombatCheckTimer : ECSGameTimerWrapperBase
        {
            private ControlledMobBrain _brain;
            private ECSPulseEffect _pulseEffect;
            private bool _hasOwnerBeenInCombat; // Set to true once and never changes again.
            private long _cancelEffectDueTime;

            public CombatCheckTimer(ControlledMobBrain brain, ECSPulseEffect pulseEffect) : base(brain.Owner)
            {
                _brain = brain;
                _pulseEffect = pulseEffect;
                Start(1); // Tick every game loop tick.
            }

            protected override int OnTick(ECSGameTimer timer)
            {
                if (!_pulseEffect.IsBuffActive)
                    return 0;

                bool isOwnerInCombat = _brain.Body.InCombatInLast((int) _pulseEffect.PulseFreq);

                // The pet needs to have entered combat at least once.
                // Prevents the effect from being canceled immediately after being applied.
                if (!_hasOwnerBeenInCombat)
                {
                    if (!isOwnerInCombat)
                        return Interval;

                    _hasOwnerBeenInCombat = true;
                }

                // Wait until we're out of combat (using pulse frequency).
                if (isOwnerInCombat)
                    return Interval;

                // If the pet is out of combat but is still moving or was ordered to attack something, restart from the beginning.
                // This way we're not cancelling the effect if the pet gets out of attack range mid-pull, or if the player asks it to attack something.
                if (_brain.Body.CurrentSpeed > 0 || _brain.OrderedAttackTarget != null)
                {
                    _hasOwnerBeenInCombat = false;
                    return Interval;
                }

                (_pulseEffect.SpellHandler as SpellHandler).CancelFocusSpells(false);
                return 0;
            }
        }
    }
}
