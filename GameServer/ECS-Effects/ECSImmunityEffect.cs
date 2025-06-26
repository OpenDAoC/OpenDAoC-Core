using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameSpellEffect
    {
        public ECSImmunityEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness)
            : base(new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            // Some of this is already done in the base constructor and should be cleaned up
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            EffectType = EffectService.GetImmunityEffectFromSpell(handler.Spell);
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            TriggersImmunity = false;
            Start();
        }

        protected ECSImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams) { }
    }

    public abstract class NpcImmunityEffect : ECSImmunityEffect
    {
        private int _count = 1;

        protected NpcImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            Owner = initParams.Target;
            Duration = 60000;
            Start();
        }

        public bool CanApplyNewEffect(long duration)
        {
            // Whether a new effect can be applied depends on its duration.
            // This is equivalent to `_count < duration / 2000 + 1`.
            // This seems to be correct for stuns. Mez are untested and may have a different threshold.
            return CalculateNewEffectDuration(duration) >= 1000;
        }

        public long CalculateNewEffectDuration(long duration)
        {
            // Duration is reduced for every new application.
            duration /= 2 * _count;
            return duration;
        }

        public void OnApplyNewEffect()
        {
            _count++;
        }
    }

    public class NpcStunImmunityEffect : NpcImmunityEffect
    {
        public NpcStunImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.NPCStunImmunity;
        }
    }

    public class NpcMezImmunityEffect : NpcImmunityEffect
    {
        public NpcMezImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.NPCMezImmunity;
        }
    }
}
