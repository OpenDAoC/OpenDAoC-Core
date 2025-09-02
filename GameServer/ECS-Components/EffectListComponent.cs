using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.Spells;
using DOL.Logging;

namespace DOL.GS
{
    public class EffectListComponent : IServiceObject
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        // Array of pulse spell groups allowed to exist with others.
        // Used to allow players to have more than one pulse spell refreshing itself automatically.
        private static readonly int[] PulseSpellGroupsIgnoringOtherPulseSpells = [];

        // Active effects.
        private readonly Dictionary<eEffect, List<ECSGameEffect>> _effects = new();  // Dictionary of effects by their type.
        private readonly Dictionary<int, ECSGameEffect> _effectIdToEffect = new();   // Dictionary of effects by their icon ID.

        // Pending effects.
        private readonly Queue<PendingEffect> _effectsToStartOrStop = new();         // Queue for effects to start or stop after their state has been finalized.

        // Concentration.
        private readonly List<ECSGameSpellEffect> _concentrationEffects = new(20);   // List of concentration effects currently active on the player.
        private int _usedConcentration;                                              // Amount of concentration used by the player.
        private readonly Lock _concentrationEffectsLock = new();                     // Lock for synchronizing access to the concentration effects list.

        // Player updates.
        private EffectHelper.PlayerUpdate _requestedPlayerUpdates;                   // Player updates requested by the effects, to be sent in the next tick.
        private int _lastUpdateEffectsCount;                                         // Number of effects sent in the last player update, used externally.
        private readonly Lock _playerUpdatesLock = new();

        private readonly Lock _processEffectLock = new();

        public GameLiving Owner { get; }
        public int UsedConcentration => Volatile.Read(ref _usedConcentration);
        public ServiceObjectId ServiceObjectId { get; set; } = new(ServiceObjectType.EffectListComponent);

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick()
        {
            while (_effectsToStartOrStop.TryDequeue(out PendingEffect pendingEffect))
                pendingEffect.Process();

            if (_effects.Count == 0)
            {
                ServiceObjectStore.Remove(this);
                SendPlayerUpdates();
                return;
            }

            // Don't check `IsAlive`, since it returns false during region change.
            // Keep ticking even if `ObjectState == Inactive` (region change).
            if (Owner.Health <= 0 || Owner.ObjectState is GameObject.eObjectState.Deleted)
                CancelAll();

            SendPlayerUpdates();
        }

        public void TryEnableBestEffectOfSameType(ECSGameEffect effect)
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
                    int radiusToCheck = EffectHelper.GetConcentrationEffectActivationRange(spellHandler.Spell.SpellType);

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
            List<ECSGameSpellEffect> temp = GameLoop.GetListForTick<ECSGameSpellEffect>();

            lock (_concentrationEffectsLock)
            {
                temp.AddRange(_concentrationEffects);
            }

            return temp;
        }

        public List<ECSGameEffect> GetEffects()
        {
            List<ECSGameEffect> temp = GameLoop.GetListForTick<ECSGameEffect>();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is not ECSPulseEffect)
                        temp.Add(effects[i]);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
        }

        public List<ECSGameEffect> GetEffects(eEffect effectType)
        {
            List<ECSGameEffect> temp = GameLoop.GetListForTick<ECSGameEffect>();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is not ECSPulseEffect)
                        temp.Add(effects[i]);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
        }

        public List<ECSPulseEffect> GetPulseEffects()
        {
            List<ECSPulseEffect> temp = GameLoop.GetListForTick<ECSPulseEffect>();

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
            List<ECSGameSpellEffect> effects = GetSpellEffects(effectType);

            if (effects == null || effects.Count == 0)
                return null;

            ECSGameSpellEffect maxEffect = null;
            double maxValue = double.MinValue;

            for (int i = 0; i < effects.Count; i++)
            {
                ECSGameSpellEffect spellEffect = effects[i];

                if (!spellEffect.IsDisabled)
                    continue;

                double currentValue = spellEffect.SpellHandler.Spell.Value;

                if (maxEffect != null && currentValue <= maxValue)
                    continue;

                maxEffect = spellEffect;
                maxValue = currentValue;
            }

            return maxEffect;
        }

        public List<ECSGameSpellEffect> GetSpellEffects()
        {
            List<ECSGameSpellEffect> temp = GameLoop.GetListForTick<ECSGameSpellEffect>();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameSpellEffect spellEffect)
                        temp.Add(spellEffect);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
        }

        public List<ECSGameSpellEffect> GetSpellEffects(eEffect effectType)
        {
            List<ECSGameSpellEffect> temp = GameLoop.GetListForTick<ECSGameSpellEffect>();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameSpellEffect spellEffect)
                        temp.Add(spellEffect);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects()
        {
            List<ECSGameAbilityEffect> temp = GameLoop.GetListForTick<ECSGameAbilityEffect>();

            foreach (var pair in _effects)
            {
                List<ECSGameEffect> effects = pair.Value;

                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameAbilityEffect abilityEffect)
                        temp.Add(abilityEffect);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects(eEffect effectType)
        {
            List<ECSGameAbilityEffect> temp = GameLoop.GetListForTick<ECSGameAbilityEffect>();

            if (_effects.TryGetValue(effectType, out List<ECSGameEffect> effects))
            {
                for (int i = effects.Count - 1; i >= 0; i--)
                {
                    if (effects[i] is ECSGameAbilityEffect abilityEffect)
                        temp.Add(abilityEffect);
                }
            }

            temp.Sort(static (e1, e2) => e1.StartTick.CompareTo(e2.StartTick));
            return temp;
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

        public void CancelIncompatiblePulseEffects(ISpellHandler spellHandler)
        {
            if (!PulseSpellGroupsIgnoringOtherPulseSpells.Contains(spellHandler.Spell.Group))
            {
                IEnumerable<ECSPulseEffect> otherPulseEffects = GetPulseEffects().Where(x => !PulseSpellGroupsIgnoringOtherPulseSpells.Contains(x.SpellHandler.Spell.Group));

                foreach (ECSPulseEffect otherPulseEffect in otherPulseEffects)
                    otherPulseEffect.Stop();
            }
        }

        public void RequestPlayerUpdate(EffectHelper.PlayerUpdate playerUpdate)
        {
            lock (_playerUpdatesLock)
            {
                _requestedPlayerUpdates |= playerUpdate;
                ServiceObjectStore.Add(this);
            }
        }

        public void ProcessEffect(ECSGameEffect effect)
        {
            lock (_processEffectLock)
            {
                ProcessEffectInternal(effect);
            }
        }

        public void HandleConcentrationEffectRangeCheck(ECSGameSpellEffect spellEffect)
        {
            ISpellHandler spellHandler = spellEffect.SpellHandler;
            GameLiving caster = spellHandler.Caster;
            GameLiving effectOwner = spellEffect.Owner;

            if (effectOwner == caster)
                return;

            int radiusToCheck = EffectHelper.GetConcentrationEffectActivationRange(spellHandler.Spell.SpellType);

            lock (_processEffectLock)
            {
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
        }

        private void ProcessEffectInternal(ECSGameEffect effect)
        {
            ServiceObjectStore.Add(this);

            if (effect.IsStarting || effect.IsEnabling)
                AddOrEnableEffect(effect);
            else if (effect.IsStopping || effect.IsDisabling)
                RemoveOrDisableEffect(effect);
            else if (log.IsErrorEnabled)
                log.Error($"Effect was added to the queue but is neither starting nor stopping: {effect.Name} ({effect.EffectType}) on {Owner}");
        }

        private void AddOrEnableEffect(ECSGameEffect effect)
        {
            AddEffectResult result = AddEffectInternal(effect);

            if (result is not AddEffectResult.Failed)
                OnEffectAddedOrEnabled(effect);
            else
                OnEffectNotAdded(effect);

            AddEffectResult AddEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    List<ECSGameEffect> existingEffects;

                    // Special handling for ability effects. They don't have a spell handler, and there's not much to validate.
                    if (effect is ECSGameAbilityEffect abilityEffect)
                    {
                        if (_effects.TryGetValue(effect.EffectType, out existingEffects))
                        {
                            foreach (ECSGameEffect existingEffect in existingEffects)
                            {
                                if (existingEffect is not ECSGameAbilityEffect existingAbilityEffect)
                                    continue;

                                // Guard, Intercept, and Protect use the names of both the source and the target.
                                // We currently allow two players to mutually benefit from these abilities.
                                if (existingAbilityEffect.Name != effect.Name)
                                    continue;

                                existingAbilityEffect.ExpireTick = abilityEffect.ExpireTick;
                                return existingAbilityEffect.IsActive ? AddEffectResult.RenewedActive : AddEffectResult.RenewedDisabled;
                            }

                            existingEffects.Add(abilityEffect);
                        }
                        else
                            _effects.TryAdd(effect.EffectType, [effect]);

                        _effectIdToEffect[effect.Icon] = effect;
                        return AddEffectResult.Added;
                    }

                    // Cancel any incompatible pulse effect.
                    if (effect is ECSPulseEffect pulseEffect)
                        CancelIncompatiblePulseEffects(pulseEffect.SpellHandler);

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

                    // Always add immunity effects. NPC ones are started at the same time as the crowd control effect.
                    // Since they share spell IDs, the code below would cause issues.
                    if (effect is ECSImmunityEffect)
                        return AddEffectResult.Added;

                    ISpellHandler newSpellHandler = effect.SpellHandler;
                    Spell newSpell = newSpellHandler.Spell;

                    // First block handles effects with the same spell ID as an already present effect (and something about poisons).
                    // Second block handles effects with a different spell ID.
                    if (existingEffects.Any(e => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)))
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

                                // How effects are refreshed is very badly implemented. New instances are created every time and we need to use them.
                                // Most of the time, we want to stop the current effect and let the new one be started normally but silently.
                                // Not calling `OnStopEffect` on the current effect and `OnStartEffect` on the new effect means some initialization may not be performed.
                                // To give some examples:
                                // * Effectiveness changes from resurrection illness expiring.
                                // * Champion debuffs being forced to spec debuffs in `OnStartEffect`.
                                // This doesn't work will pulsing charm spells, and it's probably safer to exclude every pulsing spell for now.
                                // This should also ignore effects being re-enabled.
                                // For those, we replace the instance directly, both in our list and in `ServiceObjectStore`.
                                if (!existingEffect.IsDisabled && !effect.IsEnabling && !newSpell.IsPulsing)
                                {
                                    existingEffect.IsBeingReplaced = true;

                                    // Abort the process if anything doesn't work as expected. This should be logged.
                                    if (!existingEffect.Stop(false))
                                    {
                                        existingEffect.IsBeingReplaced = false;
                                        return AddEffectResult.Failed;
                                    }

                                    effect.IsBeingReplaced = true;
                                    existingEffects.Add(effect);
                                    result = AddEffectResult.Added;
                                }
                                else
                                {
                                    ServiceObjectStore.Remove(existingEffect);
                                    ServiceObjectStore.Add(effect);
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
                                            effectsToDisable ??= GameLoop.GetListForTick<ECSGameEffect>();
                                            effectsToDisable.Add(existingEffect);
                                        }
                                    }
                                    else
                                    {
                                        effectsToStop ??= GameLoop.GetListForTick<ECSGameEffect>();
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

            void OnEffectAddedOrEnabled(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectHelper.GetPlayerUpdateFromEffect(effect.EffectType));

                    if (!effect.FinalizeState(result))
                        return;

                    _effectsToStartOrStop.Enqueue(new(effect, true));

                    if (effect is ECSGameSpellEffect spellEffect)
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
                                    EffectHelper.SendSpellAnimation(spellEffect);
                            }
                            else if (spell.IsHarmful)
                                EffectHelper.SendSpellAnimation(spellEffect);
                        }
                        else if (spellEffect is not ECSImmunityEffect)
                            EffectHelper.SendSpellAnimation(spellEffect);
                    }
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect added to {effect.Owner}'s effect list", e);
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

                    EffectHelper.SendSpellResistAnimation(spellEffect);
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

        private void RemoveOrDisableEffect(ECSGameEffect effect)
        {
            RemoveEffectResult result = RemoveOrDisableEffectInternal(effect);

            if (result is not RemoveEffectResult.Failed)
                OnEffectRemovedOrDisabled(effect);

            RemoveEffectResult RemoveOrDisableEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    if (!_effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                        return RemoveEffectResult.Failed;

                    if (effect.IsDisabling)
                        return RemoveEffectResult.Disabled;

                    if (effect.IsStopping)
                    {
                        existingEffects.Remove(effect);
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

            void OnEffectRemovedOrDisabled(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectHelper.GetPlayerUpdateFromEffect(effect.EffectType));

                    if (effect.FinalizeState(result))
                        _effectsToStartOrStop.Enqueue(new(effect, false));
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect removed from {effect.Owner}'s effect list", e);
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

            RequestPlayerUpdate(EffectHelper.PlayerUpdate.CONCENTRATION);
        }

        private void RemoveFromConcentrationEffectList(ECSGameSpellEffect spellEffect)
        {
            lock (_concentrationEffectsLock)
            {
                _usedConcentration -= spellEffect.SpellHandler.Spell.Concentration;
                _concentrationEffects.Remove(spellEffect);
            }

            RequestPlayerUpdate(EffectHelper.PlayerUpdate.CONCENTRATION);
        }

        private void SendPlayerUpdates()
        {
            if (_requestedPlayerUpdates is EffectHelper.PlayerUpdate.NONE)
                return;

            EffectHelper.PlayerUpdate requestedUpdates;

            lock (_playerUpdatesLock)
            {
                if (_requestedPlayerUpdates is EffectHelper.PlayerUpdate.NONE)
                    return;

                requestedUpdates = _requestedPlayerUpdates;
                _requestedPlayerUpdates = EffectHelper.PlayerUpdate.NONE;
            }

            if (Owner is GamePlayer playerOwner)
            {
                if ((requestedUpdates & EffectHelper.PlayerUpdate.ICONS) != 0)
                {
                    playerOwner.Group?.UpdateMember(playerOwner, true, false);
                    playerOwner.Out.SendUpdateIcons(GetEffects(), ref GetLastUpdateEffectsCount());
                }

                if ((requestedUpdates & EffectHelper.PlayerUpdate.STATUS) != 0)
                    playerOwner.Out.SendStatusUpdate();

                if ((requestedUpdates & EffectHelper.PlayerUpdate.STATS) != 0)
                    playerOwner.Out.SendCharStatsUpdate();

                if ((requestedUpdates & EffectHelper.PlayerUpdate.RESISTS) != 0)
                    playerOwner.Out.SendCharResistsUpdate();

                if ((requestedUpdates & EffectHelper.PlayerUpdate.WEAPON_ARMOR) != 0)
                    playerOwner.Out.SendUpdateWeaponAndArmorStats();

                if ((requestedUpdates & EffectHelper.PlayerUpdate.ENCUMBERANCE) != 0)
                    playerOwner.UpdateEncumbrance();

                if ((requestedUpdates & EffectHelper.PlayerUpdate.CONCENTRATION) != 0)
                    playerOwner.Out.SendConcentrationList();
            }
            else if (Owner is GameNPC npcOwner && npcOwner.Brain is IControlledBrain npcOwnerBrain)
            {
                if ((requestedUpdates & EffectHelper.PlayerUpdate.ICONS) != 0)
                    npcOwnerBrain.UpdatePetWindow();
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

        private readonly struct PendingEffect
        {
            private readonly ECSGameEffect _effect;
            private readonly bool _start;

            public PendingEffect(ECSGameEffect effect, bool start)
            {
                _effect = effect;
                _start = start;
            }

            public void Process()
            {
                if (_start)
                    _effect.OnStartEffect();
                else
                {
                    _effect.OnStopEffect();

                    if (!_effect.IsBeingReplaced)
                    {
                        _effect.TryApplyImmunity();
                        _effect.Owner.effectListComponent.TryEnableBestEffectOfSameType(_effect);
                    }
                    else
                        _effect.IsBeingReplaced = false;
                }
            }
        }
    }
}
