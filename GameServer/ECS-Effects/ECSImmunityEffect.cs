using System.Reflection;
using DOL.Logging;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameSpellEffect
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public ECSImmunityEffect(in ECSGameEffectInitParams initParams, int pulseFreq) : base(initParams)
        {
            PulseFreq = pulseFreq;
            EffectType = EffectHelper.GetImmunityEffectFromSpell(SpellHandler.Spell);

            if (EffectType is eEffect.Unknown)
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Tried to create an immunity effect for spell '{SpellHandler.Spell.Name}' ({SpellHandler.Spell.SpellType}), but no corresponding immunity effect was found.");
            }

            StartTick = GameLoop.GameLoopTime;
            TriggersImmunity = false;
        }

        protected ECSImmunityEffect(in ECSGameEffectInitParams initParams) : base(initParams) { }
    }

    public abstract class NpcImmunityEffect : ECSImmunityEffect
    {
        private int _count = 1;

        protected NpcImmunityEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            Owner = initParams.Target;
            Duration = 60000;
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
        public NpcStunImmunityEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.NPCStunImmunity;
        }
    }

    public class NpcMezImmunityEffect : NpcImmunityEffect
    {
        public NpcMezImmunityEffect(in ECSGameEffectInitParams initParams) : base(initParams)
        {
            EffectType = eEffect.NPCMezImmunity;
        }
    }
}
