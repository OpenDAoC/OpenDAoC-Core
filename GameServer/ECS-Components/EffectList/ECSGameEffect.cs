using System;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
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

        public ECSGameEffect() { }

//         public ECSGameEffect(GameLiving owner,ISpellHandler handler, int duration, int pulseFreq, double effectiveness, ushort icon, bool cancelEffect = false)
//         {
//             Owner = owner;
//             SpellHandler = handler;
//             Duration = duration;
//             //ExpireTick = 0;
//             PulseFreq = pulseFreq;
//             Effectiveness = effectiveness;
//             Icon = icon;
//             CancelEffect = cancelEffect;
//             RenewEffect = false;
//             IsDisabled = false;
//             IsBuffActive = false;
//             EffectType = MapEffect();
//             ExpireTick = duration + GameLoop.GameLoopTime;
//             StartTick = GameLoop.GameLoopTime;
//             LastTick = 0;
// 
//             if (handler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
//             {
//                 TickInterval = 650;
//                 NextTick = 1 + (duration >> 1) + (int)StartTick;
//             }
//             else if (handler.Spell.SpellType == (byte)eSpellType.HealOverTime)
//             {
//                 NextTick = StartTick;
//             }
//             else if (handler.Spell.SpellType == (byte)eSpellType.Confusion)
//             {
//                 PulseFreq = 5000;
//             }
//             else if (handler.Spell.IsConcentration)
//             {
//                 NextTick = StartTick;
//                 // 60 seconds taken from PropertyChangingSpell
//                 // Not sure if this is correct
//                 PulseFreq = 650;
//             }
//         }

        public ECSGameEffect(ISpellHandler handler, GameLiving target, int duration, double effectiveness)
        {
            Owner = target;
            SpellHandler = handler;
            Duration = duration;
            PulseFreq = handler.Spell != null ? handler.Spell.Frequency : 0;
            Effectiveness = effectiveness;
            Icon = handler.Spell.Icon;
            CancelEffect = false;
            RenewEffect = false;
            IsDisabled = false;
            IsBuffActive = false;
            EffectType = MapEffect();
            ExpireTick = duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            LastTick = 0;

            if (handler.Spell.SpellType == (byte)eSpellType.SpeedDecrease)
            {
                TickInterval = 650;
                NextTick = 1 + (duration >> 1) + (int)StartTick;
            }
            else if (handler.Spell.SpellType == (byte)eSpellType.HealOverTime)
            {
                NextTick = StartTick;
            }
            else if (handler.Spell.SpellType == (byte)eSpellType.Confusion)
            {
                PulseFreq = 5000;
            }
            else if (handler.Spell.IsConcentration)
            {
                NextTick = StartTick;
                // 60 seconds taken from PropertyChangingSpell
                // Not sure if this is correct
                PulseFreq = 650;
            }
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

        public virtual void OnStartEffect()
        {

        }
        public virtual void OnStopEffect()
        {

        }
    }
}