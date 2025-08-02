using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
    public class EffectListComponent : IServiceObject
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Active effects.
        private Dictionary<eEffect, List<ECSGameEffect>> _effects = new();  // Dictionary of effects by their type.
        private Dictionary<int, ECSGameEffect> _effectIdToEffect = new();   // Dictionary of effects by their icon ID.

        // Pending effects.
        private ConcurrentQueue<ECSGameEffect> _pendingEffects = new();     // Queue for pending effects to be processed in the next tick.
        private int _pendingEffectCount;                                    // Number of pending effects to be processed.
        private Queue<ECSGameEffect> _effectsToStop = new();                // Queue for effects to stop after their state has been finalized following removal or disabling.
        private Queue<ECSGameEffect> _effectsToStart = new();               // Queue for effects to start after their state has been finalized following addition or enabling.

        // Concentration.
        private List<ECSGameSpellEffect> _concentrationEffects = new(20);   // List of concentration effects currently active on the player.
        private int _usedConcentration;                                     // Amount of concentration used by the player.
        private readonly Lock _concentrationEffectsLock = new();            // Lock for synchronizing access to the concentration effects list.

        // Player updates.
        private EffectService.PlayerUpdate _requestedPlayerUpdates;         // Player updates requested by the effects, to be sent in the next tick.
        private int _lastUpdateEffectsCount;                                // Number of effects sent in the last player update, used externally.
        private readonly Lock _playerUpdatesLock = new();                   // Lock for synchronizing access to the requested player updates.

        public GameLiving Owner { get; }
        public int UsedConcentration => Volatile.Read(ref _usedConcentration);
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.EffectListComponent);

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void BeginTick()
        {
            // Process pending effects. This modifies local collections and finalizes the effect's immediate state.
            if (Volatile.Read(ref _pendingEffectCount) > 0)
            {
                Interlocked.Exchange(ref _pendingEffectCount, 0);

                while (_pendingEffects.TryDequeue(out ECSGameEffect effect))
                    TickPendingEffect(effect);
            }
        }

        public void EndTick()
        {
            // Every effect that had its state changed during `BeginTick` is actually started or stopped here.
            // This allows these effects to call getters on other `EffectListComponent` in a thread-safe manner.
            while (_effectsToStop.TryDequeue(out ECSGameEffect effect))
            {
                effect.OnStopEffect();
                effect.TryApplyImmunity();
                TryEnableBestEffectOfSameType(effect);
            }

            while (_effectsToStart.TryDequeue(out ECSGameEffect effect))
                effect.OnStartEffect();

            // Keep ticking even if `ObjectState == Inactive` (region change).
            if (_effects.Count == 0 || Owner.ObjectState is GameObject.eObjectState.Deleted)
            {
                ServiceObjectStore.Remove(this);
                SendPlayerUpdates();
                return;
            }

            // Don't check `IsAlive`, since it returns false during region change.
            if (Owner.Health <= 0)
            {
                CancelAll();
                SendPlayerUpdates();
                return;
            }

            // When an effect expires in `TickEffect`, it is enqueued to `_pendingEffects` and processed on the next game loop tick.
            foreach (var pair in _effects)
            {
                List<ECSGameEffect> list = pair.Value;

                for (int i = list.Count - 1; i >= 0; i--)
                    TickEffect(list[i]);
            }

            SendPlayerUpdates();

            void TryEnableBestEffectOfSameType(ECSGameEffect effect)
            {
                if (!_effects.TryGetValue(effect.EffectType, out var effects) || effects.Count == 0)
                    return;

                ECSGameEffect bestActiveEffect = null;
                ECSGameEffect disabledEffect = null;

                foreach (ECSGameEffect existingEffect in effects)
                {
                    if (existingEffect == effect || existingEffect is not ECSGameSpellEffect)
                        continue;

                    // Keep track of the best active effect.
                    if (!existingEffect.IsDisabled)
                    {
                        if (bestActiveEffect == null || existingEffect.IsBetterThan(bestActiveEffect))
                            bestActiveEffect = existingEffect;

                        continue;
                    }

                    // If this is a disabled concentration effect, check the activation range.
                    if (existingEffect.IsConcentrationEffect())
                    {
                        ISpellHandler spellHandler = existingEffect.SpellHandler;
                        int radiusToCheck = EffectService.GetConcentrationEffectActivationRange(spellHandler.Spell.SpellType);

                        if (!spellHandler.Caster.IsWithinRadius(effect.Owner, radiusToCheck))
                            continue;
                    }

                    // Keep track of the best disabled effect.
                    if (disabledEffect == null || existingEffect.IsBetterThan(disabledEffect))
                        disabledEffect = existingEffect;
                }

                // Only enable the best disabled effect if it's better than the best active effect.
                if (bestActiveEffect == null)
                    disabledEffect?.Enable();
                else if (disabledEffect != null && disabledEffect.IsBetterThan(bestActiveEffect))
                    disabledEffect.Enable();
            }
        }

        public void AddPendingEffect(ECSGameEffect effect)
        {
            Interlocked.Increment(ref _pendingEffectCount);
            _pendingEffects.Enqueue(effect);
            ServiceObjectStore.Add(this);
        }

        public void StopConcentrationEffect(int index, bool playerCancelled)
        {
            lock (_concentrationEffectsLock)
            {
                if (index < 0 || index >= _concentrationEffects.Count)
                    return;

                _concentrationEffects[index].Stop(playerCancelled);
            }
        }

        public void StopConcentrationEffects(bool playerCancelled)
        {
            lock (_concentrationEffectsLock)
            {
                for (int i = 0; i < _concentrationEffects.Count; i++)
                    _concentrationEffects[i].Stop(playerCancelled);
            }
        }

        public List<ECSGameSpellEffect> GetConcentrationEffects()
        {
            lock (_concentrationEffectsLock)
            {
                return _concentrationEffects.ToList();
            }
        }

        public List<ECSGameEffect> GetEffects()
        {
            List<ECSGameEffect> temp = new();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is not ECSPulseEffect)
                        temp.Add(effects[i]);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public List<ECSGameEffect> GetEffects(eEffect effectType)
        {
            List<ECSGameEffect> temp = new();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is not ECSPulseEffect)
                        temp.Add(effects[i]);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public List<ECSPulseEffect> GetPulseEffects()
        {
            List<ECSPulseEffect> temp = new();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSPulseEffect pulseEffect)
                        temp.Add(pulseEffect);
                }
            }

            return temp;
        }

        public ECSGameSpellEffect GetBestDisabledSpellEffect(eEffect effectType)
        {
            return GetSpellEffects(effectType)?.Where(x => x.IsDisabled).OrderByDescending(x => x.SpellHandler.Spell.Value).FirstOrDefault();
        }

        public List<ECSGameSpellEffect> GetSpellEffects()
        {
            List<ECSGameSpellEffect> temp = new();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameSpellEffect spellEffect)
                        temp.Add(spellEffect);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public List<ECSGameSpellEffect> GetSpellEffects(eEffect effectType)
        {
            List<ECSGameSpellEffect> temp = new();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameSpellEffect spellEffect)
                        temp.Add(spellEffect);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects()
        {
            List<ECSGameAbilityEffect> temp = new();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameAbilityEffect abilityEffect)
                        temp.Add(abilityEffect);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects(eEffect effectType)
        {
            List<ECSGameAbilityEffect> temp = new();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameAbilityEffect abilityEffect)
                        temp.Add(abilityEffect);
                }
            }

            return temp.OrderBy(e => e.StartTick).ToList();
        }

        public ECSGameEffect TryGetEffectFromEffectId(int effectId)
        {
            _effectIdToEffect.TryGetValue(effectId, out ECSGameEffect effect);
            return effect;
        }

        public ref int GetLastUpdateEffectsCount()
        {
            return ref _lastUpdateEffectsCount;
        }

        public bool ContainsEffectForEffectType(eEffect effectType)
        {
            return _effects.TryGetValue(effectType, out var effects) && effects.Count > 0;
        }

        public void CancelAll()
        {
            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                    effects[i].Stop();
            }
        }

        public void RequestPlayerUpdate(EffectService.PlayerUpdate playerUpdate)
        {
            lock (_playerUpdatesLock)
            {
                _requestedPlayerUpdates |= playerUpdate;
                ServiceObjectStore.Add(this);
            }
        }

        private void TickPendingEffect(ECSGameEffect effect)
        {
            if (effect.IsStarting || effect.IsEnabling)
                AddEffect(effect);
            else if (effect.IsStopping || effect.IsDisabling)
                RemoveEffect(effect);
            else if (log.IsErrorEnabled)
                log.Error($"Effect was added to the queue but is neither starting nor stopping: {effect.Name} ({effect.EffectType}) on {Owner}");
        }

        private void TickEffect(ECSGameEffect effect)
        {
            if (effect is ECSGameAbilityEffect abilityEffect)
                TickAbilityEffect(abilityEffect);
            else if (effect is ECSGameSpellEffect spellEffect)
                TickSpellEffect(spellEffect);

            static void TickAbilityEffect(ECSGameAbilityEffect abilityEffect)
            {
                if (abilityEffect.NextTick != 0 && ServiceUtils.ShouldTick(abilityEffect.NextTick))
                {
                    abilityEffect.OnEffectPulse();
                    abilityEffect.NextTick += abilityEffect.PulseFreq;
                }

                if (abilityEffect.Duration > 0 && ServiceUtils.ShouldTick(abilityEffect.ExpireTick))
                    abilityEffect.Stop();
            }

            void TickSpellEffect(ECSGameSpellEffect spellEffect)
            {
                ISpellHandler spellHandler = spellEffect.SpellHandler;
                Spell spell = spellHandler.Spell;
                GameLiving caster = spellHandler.Caster;
                bool isConcentrationEffect = spellEffect.IsConcentrationEffect() && !spell.IsFocus;

                if (isConcentrationEffect && spellEffect.IsAllowedToPulse)
                {
                    TickConcentrationEffect(spellEffect);
                    return;
                }

                if (ServiceUtils.ShouldTick(spellEffect.ExpireTick))
                {
                    // A pulse effect cancels its own child effects to prevent them from being cancelled and immediately reapplied.
                    // So only cancel them if their source is no longer active.
                    if (spellHandler.PulseEffect?.IsActive != true)
                    {
                        spellEffect.Stop();
                        return;
                    }
                }

                // Make sure the effect actually has a next tick scheduled since some spells are marked as pulsing but actually don't.
                if (spellEffect.IsAllowedToPulse)
                    TickPulsingEffect(spellEffect, spell, spellHandler, caster);

                void TickConcentrationEffect(ECSGameSpellEffect spellEffect)
                {
                    if (!ServiceUtils.ShouldTick(spellEffect.NextTick))
                        return;

                    ISpellHandler spellHandler = spellEffect.SpellHandler;
                    GameLiving caster = spellHandler.Caster;
                    GameLiving effectOwner = spellEffect.Owner;

                    if (effectOwner != caster)
                    {
                        int radiusToCheck = EffectService.GetConcentrationEffectActivationRange(spellHandler.Spell.SpellType);

                        // Check if the concentration buff needs to be enabled or disabled, based on its current state and the distance between the player and the caster.
                        if (caster.IsWithinRadius(effectOwner, radiusToCheck))
                        {
                            if (spellEffect.IsDisabled)
                            {
                                // Check if the concentration buff is better than currently enabled effects.
                                // If there isn't any other effect of this type, simply enable it.
                                if (!_effects.TryGetValue(spellEffect.EffectType, out List<ECSGameEffect> existingEffects))
                                    spellEffect.Enable();
                                else
                                {
                                    if (existingEffects.Count == 1)
                                        spellEffect.Enable();
                                    else
                                    {
                                        ECSGameEffect effectToDisable = null;

                                        // Get the weakest enabled effect.
                                        foreach (ECSGameEffect existingEnabledEffect in existingEffects)
                                        {
                                            if (spellEffect == existingEnabledEffect || !existingEnabledEffect.IsActive)
                                                continue;

                                            if (!spellEffect.IsBetterThan(existingEnabledEffect) || existingEnabledEffect is not ECSGameSpellEffect)
                                                continue;

                                            if (effectToDisable == null || existingEnabledEffect.IsBetterThan(effectToDisable))
                                                effectToDisable = existingEnabledEffect;
                                        }

                                        // We shouldn't enable the concentration effect explicitly if there are any other effect of the same type.
                                        // There is a risk of the effect being activated when it shouldn't. It's safer to let `EndTick` handles it when another similar effect is stopped.
                                        effectToDisable?.Disable();
                                    }
                                }
                            }
                        }
                        else if (spellEffect.IsActive)
                            spellEffect.Disable();
                    }

                    spellEffect.NextTick += spellEffect.PulseFreq;
                }

                static void TickPulsingEffect(ECSGameSpellEffect spellEffect, Spell spell, ISpellHandler spellHandler, GameLiving caster)
                {
                    if (!ServiceUtils.ShouldTick(spellEffect.NextTick))
                        return;

                    // Not every pulsing effect is a `ECSPulseEffect`. Snares and roots decreasing effect are also handled as pulsing spells for example.
                    if (spellEffect is ECSPulseEffect pulseEffect)
                    {
                        if (!caster.ActivePulseSpells.ContainsKey(spell.SpellType))
                            pulseEffect.Stop();
                        else
                        {
                            if (spell.PulsePower > 0)
                            {
                                if (caster.Mana >= spell.PulsePower)
                                {
                                    caster.Mana -= spell.PulsePower;
                                    spellHandler.StartSpell(null);
                                }
                                else
                                {
                                    (spellHandler as SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                    pulseEffect.Stop();
                                    return;
                                }
                            }
                            else
                                spellHandler.StartSpell(null);

                            if (spell.IsHarmful && spell.SpellType is not eSpellType.SpeedDecrease)
                            {
                                if (!pulseEffect.Owner.IsMezzed && !pulseEffect.Owner.IsStunned)
                                    (spellHandler as SpellHandler).SendCastAnimation();
                            }

                            List<GameLiving> livings = null;

                            foreach (var pair in pulseEffect.ChildEffects)
                            {
                                ECSGameSpellEffect childEffect = pair.Value;

                                if (ServiceUtils.ShouldTick(childEffect.ExpireTick))
                                {
                                    livings ??= new();
                                    livings.Add(pair.Key);
                                    childEffect.Stop();
                                }
                            }

                            if (livings != null)
                            {
                                foreach (GameLiving living in livings)
                                {
                                    pulseEffect.ChildEffects.Remove(living);
                                }
                            }
                        }
                    }
                    else if (spellEffect is not ECSImmunityEffect && spellEffect.EffectType is not eEffect.Pulse && spell.IsSnare)
                    {
                        double factor = 2.0 - (spellEffect.Duration - spellEffect.GetRemainingTimeForClient()) / (spellEffect.Duration * 0.5);

                        if (factor < 0)
                            factor = 0;
                        else if (factor > 1)
                            factor = 1;

                        factor *= spellEffect.Effectiveness; // Includes critical hit.
                        spellEffect.Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, spellEffect, 1.0 - spellEffect.SpellHandler.Spell.Value * factor * 0.01);
                        spellEffect.Owner.OnMaxSpeedChange();

                        if (factor <= 0)
                        {
                            spellEffect.Stop();
                            return;
                        }
                    }

                    spellEffect.OnEffectPulse();
                    spellEffect.NextTick += spellEffect.PulseFreq;
                }
            }
        }

        private void AddEffect(ECSGameEffect effect)
        {
            AddEffectResult result = AddEffectInternal(effect);

            if (result is not AddEffectResult.Failed)
            {
                bool shouldStart = OnEffectAdded(effect);

                if (shouldStart)
                    _effectsToStart.Enqueue(effect);
            }
            else
                OnEffectNotAdded(effect);

            AddEffectResult AddEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    List<ECSGameEffect> existingEffects;

                    if (effect is ECSGameAbilityEffect)
                    {
                        if (_effects.TryGetValue(effect.EffectType, out existingEffects))
                            existingEffects.Add(effect);
                        else
                            _effects.TryAdd(effect.EffectType, [effect]);

                        _effectIdToEffect[effect.Icon] = effect;
                        return AddEffectResult.Added;
                    }

                    // Completely new effect.
                    if (!_effects.TryGetValue(effect.EffectType, out existingEffects))
                    {
                        if (!HandleConcentration(effect as ECSGameSpellEffect))
                            return AddEffectResult.Failed;

                        _effects.TryAdd(effect.EffectType, [effect]);

                        if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                            _effectIdToEffect[effect.Icon] = effect;

                        return AddEffectResult.Added;
                    }

                    ISpellHandler newSpellHandler = effect.SpellHandler;
                    Spell newSpell = newSpellHandler.Spell;

                    // First block handles effects with the same spell ID as an already present effect (and something about poisons).
                    // Second block handles effects with a different spell ID.
                    if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)) != null)
                    {
                        if (effect.IsConcentrationEffect() && !effect.IsEnabling)
                            return AddEffectResult.Failed;

                        for (int i = 0; i < existingEffects.Count; i++)
                        {
                            ECSGameEffect existingEffect = existingEffects[i];
                            ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                            Spell existingSpell = existingSpellHandler.Spell;

                            if ((existingSpell.IsPulsing && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType) && existingSpell.ID == newSpell.ID)
                                || (existingSpell.IsConcentration && existingEffect == effect)
                                || existingSpell.ID == newSpell.ID)
                            {
                                AddEffectResult result;

                                // Most of the time, we want to stop the current effect and let the new one be started normally but silently.
                                // Not calling `OnStopEffect` on the current effect and `OnStartEffect` on the new effect means some initialization may not be performed.
                                // To give some examples:
                                // * Effectiveness changes from resurrection illness expiring.
                                // * Champion debuffs being forced to spec debuffs in `OnStartEffect`.
                                // This doesn't work will pulsing charm spells, and it's probably safer to exclude every pulsing spell for now.
                                // This should also ignore effects currently disabled.
                                if (!existingEffect.IsDisabled && !effect.IsEnabling && !newSpell.IsPulsing)
                                {
                                    // This is ugly, but we really want to stop the effect first and there isn't really any elegant way to do it.
                                    // The call to stop updates the effect's internal state and should add it to the queue.
                                    // Abort the process if anything doesn't work as expected.
                                    if (!existingEffect.Stop(false, false) || !existingEffect.IsStopping)
                                        return AddEffectResult.Failed;

                                    existingEffect.IsSilent = true;
                                    TickPendingEffect(existingEffect);
                                    effect.IsSilent = true;
                                    existingEffects.Add(effect);
                                    result = AddEffectResult.Added;
                                }
                                else
                                {
                                    effect.PreviousPosition = GetEffects().IndexOf(existingEffect);
                                    existingEffects[i] = effect;
                                    result = existingEffect.IsActive ? AddEffectResult.RenewedActive : AddEffectResult.RenewedDisabled;
                                }

                                _effects.TryAdd(effect.EffectType, existingEffects);
                                _effectIdToEffect[effect.Icon] = effect;
                                return result;
                            }
                        }

                        return AddEffectResult.Added;
                    }
                    else
                    {
                        AddEffectResult result = AddEffectResult.None;

                        if (effect.EffectType is eEffect.Bladeturn)
                        {
                            // PBT should only replace itself.
                            // Self cast BTs should never be overwritten.
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                if (newSpell.IsPulsing || existingSpell.Target is eSpellTarget.SELF)
                                    continue;

                                existingEffect.Stop();
                                result = AddEffectResult.Added;
                            }
                        }
                        else if (effect.EffectType is eEffect.AblativeArmor)
                        {
                            // Special handling for ablative effects.
                            // We use the remaining amount instead of the spell value. They also can't be added as disabled effects.
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                if (existingEffects[i] is not AblativeArmorECSGameEffect existingEffect)
                                    continue;

                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // 'Damage' represents the absorption% per hit.
                                if (newSpell.Value * AblativeArmorSpellHandler.ValidateSpellDamage((int) newSpell.Damage) <= existingEffect.RemainingValue * AblativeArmorSpellHandler.ValidateSpellDamage((int) existingSpell.Damage))
                                    continue;

                                existingEffect.Stop();
                                result = AddEffectResult.Added;
                            }
                        }
                        else
                        {
                            bool addNewEffectAsDisabled = false;
                            List<ECSGameEffect> effectsToDisable = null;
                            List<ECSGameEffect> effectsToStop = null;

                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // Check if the existing and new effects are compatible.
                                if (!existingSpellHandler.HasConflictingEffectWith(effect.SpellHandler))
                                    continue;

                                if (effect.IsBetterThan(existingEffect))
                                {
                                    if (CanCoexist(effect, existingEffect))
                                    {
                                        if (!existingEffect.IsDisabled && !existingEffect.IsDisabling)
                                        {
                                            effectsToDisable ??= new(1);
                                            effectsToDisable.Add(existingEffect);
                                        }
                                    }
                                    else
                                    {
                                        effectsToStop ??= new(1);
                                        effectsToStop.Add(existingEffect);
                                    }
                                }
                                else
                                {
                                    if (CanCoexist(effect, existingEffect))
                                    {
                                        if (!existingEffect.IsDisabled)
                                            addNewEffectAsDisabled = true;
                                    }
                                    else
                                        return AddEffectResult.Failed;
                                }
                            }

                            // We know for sure the effect will be added. We can now disable and stop weaker effects.

                            if (effectsToDisable != null)
                            {
                                foreach (ECSGameEffect effectToDisable in effectsToDisable)
                                    effectToDisable.Disable();
                            }

                            if (effectsToStop != null)
                            {
                                foreach (ECSGameEffect effectToStop in effectsToStop)
                                    effectToStop.Stop();
                            }

                            result = addNewEffectAsDisabled ? AddEffectResult.Disabled : AddEffectResult.Added;
                        }

                        if (result is AddEffectResult.None || !HandleConcentration(effect as ECSGameSpellEffect))
                            return AddEffectResult.Failed;

                        existingEffects.Add(effect);
                        _effects.TryAdd(effect.EffectType, existingEffects);

                        if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                            _effectIdToEffect[effect.Icon] = effect;

                        return result;
                    }
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed adding an effect to {effect.Owner}'s effect list", e);

                    return AddEffectResult.Failed;
                }

                static bool CanCoexist(ECSGameEffect effectA, ECSGameEffect effectB)
                {
                    if (effectA == null || effectB == null)
                        return false;

                    if (effectA.IsConcentrationEffect() && effectB.IsConcentrationEffect())
                        return false;

                    if (!effectA.SpellHandler.Spell.IsHelpful || !effectB.SpellHandler.Spell.IsHelpful)
                        return false;

                    return effectA.SpellHandler.Caster != effectB.SpellHandler.Caster ||
                        effectA.SpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects ||
                        effectB.SpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects;
                }

                bool HandleConcentration(ECSGameSpellEffect spellEffect)
                {
                    if (spellEffect == null)
                        return true;

                    if (!spellEffect.ShouldBeAddedToConcentrationList() || spellEffect.IsEnabling)
                        return true;

                    ISpellHandler spellHandler = spellEffect.SpellHandler;

                    if (!spellHandler.CheckConcentrationCost(false))
                        return false;

                    GameLiving caster = spellHandler.Caster;

                    if (caster == null)
                        return true;

                    caster.effectListComponent.AddToConcentrationEffectList(spellEffect);
                    return true;
                }
            }

            bool OnEffectAdded(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectService.GetPlayerUpdateFromEffect(effect.EffectType));
                    bool shouldStart = effect.FinalizeAddedState(result);

                    if (shouldStart && effect is ECSGameSpellEffect spellEffect)
                    {
                        ISpellHandler spellHandler = spellEffect.SpellHandler;
                        Spell spell = spellHandler?.Spell;

                        if (spell.IsPulsing)
                        {
                            // This should allow the caster to see the effect of the first tick of a beneficial pulse effect, even when recasted before the existing effect expired.
                            // It means they can spam some spells, but I consider it a good feedback for the player (example: Paladin's endurance chant).
                            // It should also allow harmful effects to be played on the targets, but not the caster (example: Reaver's PBAEs -- the flames, not the waves).
                            // It should prevent double animations too (only checking 'IsHarmful' and 'RenewEffect' would make resist chants play twice).
                            if (spellEffect is ECSPulseEffect)
                            {
                                if (!spell.IsHarmful && spell.SpellType is not eSpellType.Charm && !spellEffect.IsEnabling)
                                    EffectService.SendSpellAnimation(spellEffect);
                            }
                            else if (spell.IsHarmful)
                                EffectService.SendSpellAnimation(spellEffect);
                        }
                        else if (spellEffect is not ECSImmunityEffect)
                            EffectService.SendSpellAnimation(spellEffect);

                        if (effect is StatDebuffECSEffect && spell.CastTime == 0)
                            StatDebuffECSEffect.TryDebuffInterrupt(spell, effect.OwnerPlayer, spellHandler?.Caster);
                    }

                    return shouldStart;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect added to {effect.Owner}'s effect list", e);

                    return false;
                }
            }

            static void OnEffectNotAdded(ECSGameEffect effect)
            {
                try
                {
                    if (effect is not ECSGameSpellEffect spellEffect)
                        return;

                    // Temporarily include `BleedECSEffect` since they're set as pulsing spells in the DB, even though they should work like DoTs instead.
                    if (spellEffect.SpellHandler.Spell.IsPulsing && spellEffect is not BleedECSEffect)
                        return;

                    EffectService.SendSpellResistAnimation(spellEffect);
                    GamePlayer playerToNotify = null;

                    if (spellEffect.SpellHandler.Caster is GamePlayer playerCaster)
                        playerToNotify = playerCaster;
                    else if (spellEffect.SpellHandler.Caster is GameNPC npcCaster && npcCaster.Brain is IControlledBrain brain && brain.Owner is GamePlayer casterOwner)
                        playerToNotify = casterOwner;

                    if (playerToNotify != null)
                        ChatUtil.SendResistMessage(playerToNotify, "GamePlayer.Caster.Buff.EffectAlreadyActive", spellEffect.Owner.GetName(0, true));
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect not added to {effect.Owner}'s effect list", e);
                }
            }
        }

        private void RemoveEffect(ECSGameEffect effect)
        {
            RemoveEffectResult result = RemoveEffectInternal(effect);

            if (result is not RemoveEffectResult.Failed)
            {
                bool shouldBeStopped = OnEffectRemoved(effect);

                if (shouldBeStopped)
                    _effectsToStop.Enqueue(effect);
            }

            RemoveEffectResult RemoveEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    if (!_effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                        return RemoveEffectResult.Failed;

                    if (effect.IsDisabling)
                        return RemoveEffectResult.Disabled;

                    if (effect.IsStopping)
                    {
                        // Get the effectToRemove from the Effects list. Had issues trying to remove the effect directly from the list if it wasn't the same object.
                        ECSGameEffect effectToRemove = existingEffects.FirstOrDefault(e => e.Name == effect.Name);
                        existingEffects.Remove(effectToRemove);
                        _effectIdToEffect.Remove(effect.Icon);

                        if (existingEffects.Count == 0)
                            _effects.Remove(effect.EffectType);

                        HandleConcentration(effect as ECSGameSpellEffect);
                        return RemoveEffectResult.Removed;
                    }

                    return RemoveEffectResult.Failed;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed removing an effect from {effect.Owner}'s effect list", e);

                    return RemoveEffectResult.Failed;
                }

                void HandleConcentration(ECSGameSpellEffect spellEffect)
                {
                    if (spellEffect == null)
                        return;

                    ISpellHandler spellHandler = spellEffect.SpellHandler;
                    GameLiving caster = spellHandler?.Caster;

                    if (!spellEffect.ShouldBeRemovedFromConcentrationList() || caster == null)
                        return;

                    caster.effectListComponent.RemoveFromConcentrationEffectList(spellEffect);
                }
            }

            bool OnEffectRemoved(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectService.GetPlayerUpdateFromEffect(effect.EffectType));
                    return effect.FinalizeRemovedState(result);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect removed from {effect.Owner}'s effect list", e);

                    return false;
                }
            }
        }

        private void AddToConcentrationEffectList(ECSGameSpellEffect spellEffect)
        {
            lock (_concentrationEffectsLock)
            {
                _usedConcentration += spellEffect.SpellHandler.Spell.Concentration;
                _concentrationEffects.Add(spellEffect);
            }

            RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
        }

        private void RemoveFromConcentrationEffectList(ECSGameSpellEffect spellEffect)
        {
            lock (_concentrationEffectsLock)
            {
                _usedConcentration -= spellEffect.SpellHandler.Spell.Concentration;
                _concentrationEffects.Remove(spellEffect);
            }

            RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
        }

        private void SendPlayerUpdates()
        {
            if (_requestedPlayerUpdates is EffectService.PlayerUpdate.NONE)
                return;

            lock (_playerUpdatesLock)
            {
                if (Owner is GamePlayer playerOwner)
                {
                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.ICONS) != 0)
                    {
                        playerOwner.Group?.UpdateMember(playerOwner, true, false);
                        playerOwner.Out.SendUpdateIcons(GetEffects(), ref GetLastUpdateEffectsCount());
                    }

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.STATUS) != 0)
                        playerOwner.Out.SendStatusUpdate();

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.STATS) != 0)
                        playerOwner.Out.SendCharStatsUpdate();

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.RESISTS) != 0)
                        playerOwner.Out.SendCharResistsUpdate();

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.WEAPON_ARMOR) != 0)
                        playerOwner.Out.SendUpdateWeaponAndArmorStats();

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.ENCUMBERANCE) != 0)
                        playerOwner.UpdateEncumbrance();

                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.CONCENTRATION) != 0)
                        playerOwner.Out.SendConcentrationList();
                }
                else if (Owner is GameNPC npcOwner && npcOwner.Brain is IControlledBrain npcOwnerBrain)
                {
                    if ((_requestedPlayerUpdates & EffectService.PlayerUpdate.ICONS) != 0)
                        npcOwnerBrain.UpdatePetWindow();
                }

                _requestedPlayerUpdates = EffectService.PlayerUpdate.NONE;
            }
        }

        public enum AddEffectResult
        {
            None,
            Added,
            Disabled,
            RenewedActive,
            RenewedDisabled,
            Failed
        }

        public enum RemoveEffectResult
        {
            None,
            Removed,
            Disabled,
            Failed
        }
    }
}
