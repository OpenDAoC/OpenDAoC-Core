using System;
using DOL.Database;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    public struct ECSGameEffectInitParams
    {
        public ECSGameEffectInitParams(GameLiving target, int duration, double effectiveness, ISpellHandler spellHandler = null)
        {
            
            Target = target;
            Duration = duration;
            Effectiveness = effectiveness;
            SpellHandler = spellHandler;
        }
        public GameLiving Target { get; set; }
        public int Duration { get; set; }
        public double Effectiveness { get; set; }
        public ISpellHandler SpellHandler { get; set; }
    }

    /// <summary>
    /// Base class for all Effects
    /// </summary>
    public class ECSGameEffect
    {
        //public ISpellHandler SpellHandler;
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

        /// <summary>
		/// The icon for this effect.
		/// </summary>
        public virtual ushort Icon { get { return (ushort)0; } }

        /// <summary>
        /// The name of this effect.
        /// </summary>
        public virtual string Name { get { return "Default Effect Name"; } }

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

        /// <summary>
        /// Whether this effect is positive.
        /// </summary>
        public virtual bool HasPositiveEffect { get { return false; } }

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
            OwnerPlayer = Owner as GamePlayer; // will be null on NPCs, but here for convenience.
            EffectType = eEffect.Unknown; // should be overridden in subclasses
            CancelEffect = false;
            RenewEffect = false;
            IsDisabled = false;
            IsBuffActive = false;
            ExpireTick = Duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;
            NextTick = 0;
        }

        public virtual long GetRemainingTimeForClient()
        {
            if (Duration > 0)
                return (ExpireTick - GameLoop.GameLoopTime);
            else
                return 0;
        }

        public virtual bool IsConcentrationEffect() { return false; }
        public virtual bool ShouldBeAddedToConcentrationList() { return false; }
        public virtual bool ShouldBeRemovedFromConcentrationList() { return false; }
        public virtual void TryApplyImmunity() { }
        public virtual void OnStartEffect() { }
        public virtual void OnStopEffect() { }
        public virtual void OnEffectPulse() { }

        public virtual PlayerXEffect getSavedEffect() { return null; }
    }
}