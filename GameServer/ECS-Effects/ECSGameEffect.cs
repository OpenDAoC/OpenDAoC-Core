using System;
using System.Threading;
using DOL.AI.Brain;
using DOL.Database;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Language;

namespace DOL.GS
{
    public readonly ref struct ECSGameEffectInitParams
    {
        public readonly GameLiving Target { get; }
        public readonly int Duration { get; }
        public readonly double Effectiveness { get; }
        public readonly ISpellHandler SpellHandler { get; }

        public ECSGameEffectInitParams(GameLiving target, int duration, double effectiveness, ISpellHandler spellHandler = null)
        {
            Target = target;
            Duration = duration;
            Effectiveness = effectiveness;
            SpellHandler = spellHandler;
        }
    }

    public class ECSGameEffect
    {
        private State _state;
        private TransitionalState _transitionalState;

        public long ExpireTick;
        public long StartTick;
        public long Duration;
        public long PulseFreq;
        public double Effectiveness;
        public bool Critical;
        public eEffect EffectType;
        public GameLiving Owner;
        public GamePlayer OwnerPlayer;
        public long NextTick;
        public int PreviousPosition = -1;
        public Lock StartStopLock { get; } = new();
        public ISpellHandler SpellHandler { get; protected set; }
        public virtual ushort Icon => 0;
        public virtual string Name => "Default Effect Name";
        public virtual string OwnerName => Owner != null ? Owner.Name : string.Empty;
        public virtual bool HasPositiveEffect => false;
        public bool TriggersImmunity { get; set; } = false;
        public int ImmunityDuration { get; protected set; } = 60000;
        public bool IsSilent { get; set; } // Used externally to force an effect to be silent before being enabled or disabled. Resets itself.

        // State properties.
        public bool IsActive => _state is State.Active;
        public bool IsDisabled => _state is State.Disabled;
        public bool IsStopped => _state is State.Stopped;

        // Transitional state properties.
        public bool CanChangeState => _transitionalState is TransitionalState.None;
        public bool IsStarting => _transitionalState is TransitionalState.Starting;
        public bool IsEnabling => _transitionalState is TransitionalState.Enabling;
        public bool IsDisabling => _transitionalState is TransitionalState.Disabling;
        public bool IsStopping => _transitionalState is TransitionalState.Stopping;

        // Actionability properties.
        // Checking `CanChangeState` helps preventing stack overflows when two effects are trying to modify each other (e.g. Protect).
        public bool CanStart => _state is State.None && CanChangeState;
        public bool CanBeDisabled => IsActive && CanChangeState;
        public bool CanBeEnabled => IsDisabled && CanChangeState;
        public bool CanBeStopped => (IsActive || IsDisabled) && CanChangeState;

        public ECSGameEffect() { }

        public ECSGameEffect(in ECSGameEffectInitParams initParams)
        {
            Owner = initParams.Target;
            Duration = initParams.Duration;
            Effectiveness = initParams.Effectiveness;
            OwnerPlayer = Owner as GamePlayer; // will be null on NPCs, but here for convenience.
            EffectType = eEffect.Unknown; // should be overridden in subclasses
            ExpireTick = Duration + GameLoop.GameLoopTime;
            StartTick = GameLoop.GameLoopTime;
            SpellHandler = initParams.SpellHandler;
        }

        public bool Start()
        {
            // Automatically called by the constructor.

            if (!CanStart)
                return false;

            lock (StartStopLock)
            {
                if (!CanStart)
                    return false;

                _transitionalState = TransitionalState.Starting;
                Owner.effectListComponent.AddPendingEffect(this);
                return true;
            }
        }

        public virtual bool Enable()
        {
            if (!CanBeEnabled)
                return false;

            lock (StartStopLock)
            {
                if (!CanBeEnabled)
                    return false;

                _transitionalState = TransitionalState.Enabling;
                Owner.effectListComponent.AddPendingEffect(this);
                return true;
            }
        }

        public bool Disable()
        {
            if (!CanBeDisabled)
                return false;

            lock (StartStopLock)
            {
                if (!CanBeDisabled)
                    return false;

                _transitionalState = TransitionalState.Disabling;
                Owner.effectListComponent.AddPendingEffect(this);
                return true;
            }
        }

        public bool Stop(bool playerCanceled = false, bool addToPendingEffects = true)
        {
            // `addToPendingEffects` should only be set to false when the effect is intended to be removed immediately and manually.

            if (!CanBeStopped)
                return false;

            lock (StartStopLock)
            {
                if (!CanBeStopped)
                    return false;

                // Player can't remove negative or immunity effects.
                if (playerCanceled && ((!HasPositiveEffect) || this is ECSImmunityEffect))
                {
                    if (Owner is GamePlayer player)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "Effects.GameSpellEffect.CantRemoveEffect"), eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    return false;
                }

                _transitionalState = TransitionalState.Stopping;

                if (addToPendingEffects)
                    Owner.effectListComponent.AddPendingEffect(this);

                return true;
            }
        }

        /// <summary>
        /// Sends Spell messages to all nearby/associated players when an ability/spell/style effect becomes active on a target.
        /// </summary>
        /// <param name="msgTarget">If 'true', the system sends a first-person spell message to the target/owner of the effect.</param>
        /// <param name="msgSelf">If 'true', the system sends a third-person spell message to the caster triggering the effect, regardless of their proximity to the target.</param>
        /// <param name="msgArea">If 'true', the system sends a third-person message to all players within range of the target.</param>
        public void OnEffectStartsMsg(bool msgTarget, bool msgSelf, bool msgArea)
        {
            if (!IsSilent)
                SendMessages(msgTarget, msgSelf, msgArea, SpellHandler.Spell.Message1, SpellHandler.Spell.Message2);
            else
                IsSilent = false;
        }

        /// <summary>
        /// Sends Spell messages to all nearby/associated players when an ability/spell/style effect ends on a target.
        /// </summary>
        /// <param name="msgTarget">If 'true', the system sends a first-person spell message to the target/owner of the effect.</param>
        /// <param name="msgSelf">If 'true', the system sends a third-person spell message to the caster triggering the effect, regardless of their proximity to the target.</param>
        /// <param name="msgArea">If 'true', the system sends a third-person message to all players within range of the target.</param>
        public void OnEffectExpiresMsg(bool msgTarget, bool msgSelf, bool msgArea)
        {
            if (!IsSilent)
                SendMessages(msgTarget, msgSelf, msgArea, SpellHandler.Spell.Message3, SpellHandler.Spell.Message4);
            else
                IsSilent = false;
        }

        public virtual long GetRemainingTimeForClient()
        {
            return Duration > 0 ? ExpireTick - GameLoop.GameLoopTime : 0;
        }

        public virtual bool IsBetterThan(ECSGameEffect effect)
        {
            return SpellHandler.Spell.Value * Effectiveness > effect.SpellHandler.Spell.Value * effect.Effectiveness ||
                SpellHandler.Spell.Damage * Effectiveness > effect.SpellHandler.Spell.Damage * effect.Effectiveness;
        }

        public virtual bool IsConcentrationEffect() { return false; }
        public virtual bool ShouldBeAddedToConcentrationList() { return false; }
        public virtual bool ShouldBeRemovedFromConcentrationList() { return false; }
        public virtual void TryApplyImmunity() { }
        public virtual void OnStartEffect() { }
        public virtual void OnStopEffect() { }
        public virtual void OnEffectPulse() { }
        public virtual DbPlayerXEffect GetSavedEffect() { return null; }

        public virtual bool FinalizeAddedState(EffectListComponent.AddEffectResult result)
        {
            // Returns true if the effect needs to be started.
            try
            {
                switch (result)
                {
                    case EffectListComponent.AddEffectResult.Added:
                    {
                        _state = State.Active;
                        return true;
                    }
                    case EffectListComponent.AddEffectResult.RenewedActive:
                    {
                        _state = State.Active;
                        return false;
                    }
                    case EffectListComponent.AddEffectResult.Disabled:
                    {
                        _state = State.Disabled;
                        return false;
                    }
                    case EffectListComponent.AddEffectResult.RenewedDisabled:
                    {
                        if (IsDisabled)
                        {
                            _state = State.Active;
                            return true;
                        }
                        else
                        {
                            _state = State.Disabled;
                            return false;
                        }
                    }
                    case EffectListComponent.AddEffectResult.Failed:
                        return false;
                    default:
                        throw new InvalidOperationException($"Unhandled result: {result}.");
                }
            }
            finally
            {
                _transitionalState = TransitionalState.None;
            }
        }

        public bool FinalizeRemovedState(EffectListComponent.RemoveEffectResult result)
        {
            // Returns true if the effect needs to be stopped.
            try
            {
                switch (result)
                {
                    case EffectListComponent.RemoveEffectResult.Removed:
                    {
                        bool shouldBeStopped = IsActive;
                        _state = State.Stopped;
                        return shouldBeStopped;
                    }
                    case EffectListComponent.RemoveEffectResult.Disabled:
                    {
                        bool shouldBeStopped = IsActive;
                        _state = State.Disabled;
                        return shouldBeStopped;
                    }
                    case EffectListComponent.RemoveEffectResult.Failed:
                        return false;
                    default:
                        throw new InvalidOperationException($"Unhandled result: {result}.");
                }
            }
            finally
            {
                _transitionalState = TransitionalState.None;
            }
        }

        private void SendMessages(bool msgTarget, bool msgSelf, bool msgArea, string firstPersonMessage, string thirdPersonMessage)
        {
            // Sends a first-person message directly to the caster's target, if they are a player.
            if (msgTarget && Owner is GamePlayer playerTarget)
                // "You feel more dexterous!"
                ((SpellHandler) SpellHandler).MessageToLiving(playerTarget, firstPersonMessage, eChatType.CT_Spell);

            GameLiving toExclude = null; // Either the caster or the owner if it's a pet.

            // Sends a third-person message directly to the caster to indicate the spell had landed, regardless of range.
            if (msgSelf && SpellHandler.Caster != Owner)
            {
                ((SpellHandler) SpellHandler).MessageToCaster(Util.MakeSentence(thirdPersonMessage, Owner.GetName(0, true)), eChatType.CT_Spell);

                if (SpellHandler.Caster is GamePlayer)
                    toExclude = SpellHandler.Caster;
                else if (SpellHandler.Caster is GameNPC pet && pet.Brain is ControlledMobBrain petBrain)
                {
                    GamePlayer playerOwner = petBrain.GetPlayerOwner();

                    if (playerOwner != null)
                        toExclude = playerOwner;
                }
            }

            // Sends a third-person message to all players surrounding the target.
            if (msgArea)
            {
                if (SpellHandler.Caster == Owner && SpellHandler.Caster is GamePlayer)
                    toExclude = SpellHandler.Caster;

                // "{0} looks more agile!"
                Message.SystemToArea(Owner, Util.MakeSentence(thirdPersonMessage, Owner.GetName(0, thirdPersonMessage.StartsWith("{0}"))), eChatType.CT_Spell, Owner, toExclude);
            }
        }

        private enum State
        {
            None,
            Active,
            Disabled,
            Stopped
        }

        private enum TransitionalState
        {
            None,
            Starting,
            Enabling,
            Disabling,
            Stopping
        }
    }
}
