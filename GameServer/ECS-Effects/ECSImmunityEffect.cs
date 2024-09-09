using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameSpellEffect
    {
        public ECSImmunityEffect(GameLiving owner, ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
            : base(new ECSGameEffectInitParams(owner, duration, effectiveness, handler))
        {
            // Some of this is already done in the base constructor and should be cleaned up
            Owner = owner;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = pulseFreq;
            Effectiveness = effectiveness;
            CancelEffect = cancelEffect;
            EffectType = EffectService.GetImmunityEffectFromSpell(handler.Spell);
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            TriggersImmunity = false;

            EffectService.RequestStartEffect(this);
        }

        protected ECSImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams) { }
    }

    public class NPCECSStunImmunityEffect : ECSImmunityEffect
    {
        private int _timesStunned = 1;

        public NPCECSStunImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            Owner = initParams.Target;
            Duration = 60000;
            EffectType = eEffect.NPCStunImmunity;
            EffectService.RequestStartEffect(this);
        }

        public long CalculateStunDuration(long duration)
        {
            var retVal = duration / (2 * _timesStunned);
            _timesStunned++;
            return retVal;
        }
    }

    public class NPCECSMezImmunityEffect : ECSImmunityEffect
    {
        private int _timesMezzed = 1;

        public NPCECSMezImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams)
        {
            Owner = initParams.Target;
            Duration = 60000;
            EffectType = eEffect.NPCMezImmunity;
            EffectService.RequestStartEffect(this);
        }

        public long CalculateMezDuration(long duration)
        {
            var retVal = duration / (2 * _timesMezzed);
            _timesMezzed++;
            return retVal;
        }
    }
}
