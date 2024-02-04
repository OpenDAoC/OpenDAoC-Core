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
            EffectType = MapImmunityEffect();
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            TriggersImmunity = false;

            EffectService.RequestStartEffect(this);
        }

        protected ECSImmunityEffect(ECSGameEffectInitParams initParams) : base(initParams) { }

        protected eEffect MapImmunityEffect()
        {
            switch (SpellHandler.Spell.SpellType)
            {
                case eSpellType.Mesmerize:
                    return eEffect.MezImmunity;
                case eSpellType.StyleStun:
                case eSpellType.Stun:
                    return eEffect.StunImmunity;
                case eSpellType.SpeedDecrease:
                case eSpellType.DamageSpeedDecreaseNoVariance:
                case eSpellType.DamageSpeedDecrease:
                    return eEffect.SnareImmunity;
                case eSpellType.Nearsight:
                    return eEffect.NearsightImmunity;
                default:
                    return eEffect.Unknown;
            }
        }
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
