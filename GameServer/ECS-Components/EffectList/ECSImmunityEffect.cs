using System;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class ECSImmunityEffect : ECSGameSpellEffect
    {
        public override ushort Icon { get { return SpellHandler.Spell.Icon; } }
        public override string Name { get { return SpellHandler.Spell.Name; } }

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
            LastTick = 0;
            TriggersImmunity = false;

            EffectService.RequestStartEffect(this);
        }

        protected eEffect MapImmunityEffect()
        {
            switch (SpellHandler.Spell.SpellType)
            {
                case (byte)eSpellType.Mesmerize:
                    return eEffect.MezImmunity;
                case (byte)eSpellType.StyleStun:
                case (byte)eSpellType.Stun:
                    return eEffect.StunImmunity;
                case (byte)eSpellType.SpeedDecrease:
                case (byte)eSpellType.DamageSpeedDecrease:
                    return eEffect.SnareImmunity;
                case (byte)eSpellType.Nearsight:
                    return eEffect.NearsightImmunity;
                default:
                    return eEffect.Unknown;
            }
        }
    }
    public class NPCECSStunImmunityEffect : ECSGameEffect
    {
        private int timesStunned = 1;
        public NPCECSStunImmunityEffect(ECSGameEffectInitParams initParams) : base()
        {
            Owner = initParams.Target;
            Duration = 60000;
            EffectType = eEffect.NPCStunImmunity;
            EffectService.RequestStartEffect(this);
        }

        public long CalclulateStunDuration(long duration)
        {
            var retVal = duration / (2 * timesStunned);
            timesStunned++;
            return retVal;
        }
    }

    public class NPCECSMezImmunityEffect : ECSGameEffect
    {
        private int timesStunned = 1;
        public NPCECSMezImmunityEffect(ECSGameEffectInitParams initParams) : base()
        {
            Owner = initParams.Target;
            Duration = 60000;
            EffectType = eEffect.NPCMezImmunity;
            EffectService.RequestStartEffect(this);
        }

        public long CalclulateStunDuration(long duration)
        {
            var retVal = duration / (2 * timesStunned);
            timesStunned++;
            return retVal;
        }
    }
}