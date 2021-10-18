using System;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    public struct ECSGameEffectInitParams
    {
       public ECSGameEffectInitParams(GameLiving target, int duration, double effectiveness, ISpellHandler handler)
        {
            Target = target;
            Duration = duration;
            Effectiveness = effectiveness;
            Handler = handler;
        }
        public GameLiving Target { get; set; }
        public int Duration { get; set; }
        public double Effectiveness { get; set; }
        public ISpellHandler Handler { get; set; }
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
        public bool CancelEffect;
        public bool RenewEffect;
        public bool IsDisabled;
        public bool IsBuffActive;
        public eEffect EffectType;
        public GameLiving Owner;
        public GamePlayer OwnerPlayer;
        public int TickInterval;
        public long NextTick;
        public int PreviousPosition = -1;

        string IConcentrationEffect.Name => Name;
        ushort IConcentrationEffect.Icon => Icon;
        byte IConcentrationEffect.Concentration => SpellHandler.Spell.Concentration;

        /// <summary>
		/// The icon for this effect. Try to use the spell's icon by default. Non-spell based effects override this to provide the correct icon.
		/// </summary>
        public virtual ushort Icon
        {
            get { return SpellHandler == null ? (ushort)0 : SpellHandler.Spell.Icon; }
        }

        /// <summary>
        /// The name of this effect. Try to use the spell's name by default. Non-spell based effects override this to provide the correct name.
        /// </summary>
        public virtual string Name
        {
            get { return SpellHandler == null ? "Default Effect Name" : SpellHandler.Spell.Name; }
        }

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

        public virtual bool HasPositiveEffect
        {
            get { return SpellHandler == null ? false : SpellHandler.HasPositiveEffect; }
        }

        public bool FromSpell
        {
            get { return SpellHandler != null; }
        }

        /// Whether this effect should trigger an immunity when it expires.
        public bool TriggersImmunity = false;

        /// Duration of the immunity effect, in milliseconds. Defaults to 60s.
        public int ImmunityDuration = 60000;

        public ECSGameEffect() { }

        public ECSGameEffect(ECSGameEffectInitParams initParams)
        {
            Owner = initParams.Target;
            Duration = initParams.Duration;
            Effectiveness = initParams.Effectiveness;
            SpellHandler = initParams.Handler;

            OwnerPlayer = Owner as GamePlayer; // will be null on NPCs, but here for convenience.

            CancelEffect = false;
            RenewEffect = false;
            IsDisabled = false;
            IsBuffActive = false;

            EffectType = MapEffect();
            ExpireTick = Duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;
            NextTick = 0;

            if (FromSpell)
            {
                PulseFreq = SpellHandler.Spell != null ? SpellHandler.Spell.Frequency : 0;

                if (SpellHandler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
                {
                    TickInterval = 650;
                    NextTick = 1 + (Duration >> 1) + (int)StartTick;
                }
                else if (SpellHandler.Spell.SpellType == (byte)eSpellType.HealOverTime)
                {
                    NextTick = StartTick;
                }
                else if (SpellHandler.Spell.IsConcentration)
                {
                    NextTick = StartTick;
                    // 60 seconds taken from PropertyChangingSpell
                    // Not sure if this is correct
                    PulseFreq = 650;
                }
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
            return !FromSpell ? false : SpellHandler.Spell.IsConcentration;
        }

        public bool ShouldBeAddedToConcentrationList()
        {
            return !FromSpell ? false : SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        public bool ShouldBeRemovedFromConcentrationList()
        {
            return !FromSpell ? false : SpellHandler.Spell.IsConcentration || EffectType == eEffect.Pulse;
        }

        protected virtual eEffect MapEffect()
        {
            if (!FromSpell)
                return eEffect.Unknown;

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
            if (TriggersImmunity && OwnerPlayer != null)
            {
                ECSImmunityEffect immunityEffect = new ECSImmunityEffect(Owner, SpellHandler, ImmunityDuration, (int)PulseFreq, Effectiveness, Icon);
                EntityManager.AddEffect(immunityEffect);
            }
        }

        public virtual void OnStartEffect() { }
        public virtual void OnStopEffect() { }
        public virtual void OnEffectPulse() { }
    }
}