using System;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    public struct ECSGameEffectInitParams
    {
       public ECSGameEffectInitParams(ISpellHandler handler, GameLiving target, int duration, double effectiveness)
        {
            Handler = handler;
            Target = target;
            Duration = duration;
            Effectiveness = effectiveness;
        }
        public ISpellHandler Handler { get; set; }
        public GameLiving Target { get; set; }
        public int Duration { get; set; }
        public double Effectiveness { get; set; }
    }
    
    public class ECSGameEffect : IConcentrationEffect
    {
        public ISpellHandler SpellHandler;
        //Based on GameLoop expire tick
        public long ExpireTick;
        public long StartTick;
        public long LastTick;
        public long Duration;
        public long PulseFreq;
        public double Effectiveness;
        public ushort Icon;
        public bool CancelEffect;
        public bool RenewEffect;
        public bool IsDisabled;
        public bool IsBuffActive;
        public eEffect EffectType;
        public GameLiving Owner;
        public int TickInterval;
        public long NextTick;
        public int PreviousPosition = -1;

        string IConcentrationEffect.Name => SpellHandler.Spell.Name;
        //string IConcentrationEffect.OwnerName => Owner.Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;

        /// <summary>
		/// The name of the owner
		/// </summary>
		public virtual string OwnerName
        {
            get
            {
                if (Owner != null)
                    return Owner.Name;

                return string.Empty;
            }
        }

        /// Whether this effect should trigger an immunity when it expires.
        public bool TriggersImmunity = false;

        /// Duration of the immunity effect, in milliseconds. Defaults to 60s.
        public int ImmunityDuration = 60000;

        public ECSGameEffect() { }

        public ECSGameEffect(ECSGameEffectInitParams initParams)
        {
            Owner = initParams.Target;
            SpellHandler = initParams.Handler;
            Duration = initParams.Duration;
            Effectiveness = initParams.Effectiveness;

            PulseFreq = SpellHandler.Spell != null ? SpellHandler.Spell.Frequency : 0;
            Icon = SpellHandler.Spell.Icon;
            CancelEffect = false;
            RenewEffect = false;
            IsDisabled = false;
            IsBuffActive = false;
            EffectType = MapEffect();
            ExpireTick = Duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            if (SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (Duration >> 1) + (int)StartTick;
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.HealOverTime)
            {
                NextTick = StartTick;
            }
            else if (SpellHandler.Spell.SpellType == (byte)eSpellType.Confusion)
            {
                PulseFreq = 5000;
            }
            else if (SpellHandler.Spell.IsConcentration)
            {
                NextTick = StartTick;
                // 60 seconds taken from PropertyChangingSpell
                // Not sure if this is correct
                PulseFreq = 650;
            }

            EntityManager.AddEffect(this);
        }

        public long GetRemainingTimeForClient()
        {
            if (Duration > 0)
                return (ExpireTick - GameLoop.GameLoopTime);
            else
                return 0;
        }

        public bool IsConcentrationEffect()
        {
            return SpellHandler.Spell.IsConcentration;
        }

        public bool ShouldBeAddedToConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public bool ShouldBeRemovedFromConcentrationList()
        {
            return SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        protected eEffect MapEffect()
        {
            if (SpellHandler.SpellLine.IsBaseLine)
            {
                SpellHandler.Spell.IsSpec = false;
            } else
            {
                SpellHandler.Spell.IsSpec = true;
            }

            return EffectService.GetEffectFromSpell(SpellHandler.Spell);
        }

        public virtual void TryApplyImmunity()
        {
            if (TriggersImmunity)
            {
                ECSImmunityEffect immunityEffect = new ECSImmunityEffect(Owner, SpellHandler, ImmunityDuration, (int)PulseFreq, Effectiveness, Icon);
                EntityManager.AddEffect(immunityEffect);
            }
        }

        public virtual void OnStartEffect() { }
        public virtual void OnStopEffect() { }
    }
}