using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using ECS.Debug;

namespace DOL.GS
{
    // Component for holding persistent effects on the player.
    public class EffectListComponent
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private int _lastUpdateEffectsCount;
        private int _usedConcentration;
        private Dictionary<int, ECSGameEffect> _effectIdToEffect = [];
        private EffectService.PlayerUpdate _requestedPlayerUpdates;
        private readonly Lock _playerUpdatesLock = new();

        public GameLiving Owner { get; }
        public Dictionary<eEffect, List<ECSGameEffect>> Effects { get; } = [];
        public readonly Lock EffectsLock = new();
        public List<ECSGameSpellEffect> ConcentrationEffects { get; } = new List<ECSGameSpellEffect>(20);
        public readonly Lock ConcentrationEffectsLock = new();
        public int UsedConcentration => Interlocked.CompareExchange(ref _usedConcentration, 0, 0);

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick()
        {
            long startTick = GameLoop.GetCurrentTime();

            if (Effects.Count != 0)
            {
                if (!Owner.IsAlive)
                    CancelAll();
                else
                {
                    foreach (List<ECSGameEffect> list in Effects.Values.ToList())
                    {
                        for (int i = list.Count - 1; i >= 0; i--)
                            TickEffect(list[i]);
                    }
                }
            }

            SendPlayerUpdates();
            long stopTick = GameLoop.GetCurrentTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {nameof(EffectListComponent)}.{nameof(Tick)} for {Owner.Name}({Owner.ObjectID}) Time: {stopTick - startTick}ms");
        }

        public AddEffectResult AddEffect(ECSGameEffect effect)
        {
            AddEffectResult result;

            lock (EffectsLock)
            {
                result = AddEffectInternal(effect);
            }

            if (result is not AddEffectResult.Failed)
                OnEffectAdded(effect);
            else
                OnEffectNotAdded(effect);

            return result;

            AddEffectResult AddEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    // Dead owners don't get effects
                    if (!Owner.IsAlive)
                        return AddEffectResult.Failed;

                    if (effect is ECSGameAbilityEffect)
                    {
                        if (Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                            existingEffects.Add(effect);
                        else
                            Effects.TryAdd(effect.EffectType, [effect]);

                        _effectIdToEffect[effect.Icon] = effect;
                        return AddEffectResult.Added;
                    }

                    if (effect != null && Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingGameEffects))
                    {
                        ISpellHandler newSpellHandler = effect.SpellHandler;
                        Spell newSpell = newSpellHandler.Spell;

                        // Effects contains this effect already so refresh it
                        if (existingGameEffects.FirstOrDefault((ECSGameEffect e) => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)) != null)
                        {
                            if (effect.IsConcentrationEffect() && !effect.IsEnabling)
                                return AddEffectResult.Failed;

                            for (int i = 0; i < existingGameEffects.Count; i++)
                            {
                                ECSGameEffect existingEffect = existingGameEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                if ((existingSpell.IsPulsing && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType) && existingSpell.ID == newSpell.ID)
                                    || (existingSpell.IsConcentration && existingEffect == effect)
                                    || existingSpell.ID == newSpell.ID)
                                {
                                    if (newSpell.IsPoisonEffect && effect.EffectType is eEffect.DamageOverTime)
                                    {
                                        existingEffect.ExpireTick = effect.ExpireTick;
                                        return AddEffectResult.RenewedActive;
                                    }
                                    else
                                    {
                                        AddEffectResult result;

                                        // Most of the time, we want to stop the current effect and let the new one be started normally but silently.
                                        // Not calling `OnStopEffect` on the current effect and `OnStartEffect` on the new effect means some initialization may not be performed.
                                        // To give some examples:
                                        // * Effectiveness changes from resurrection illness expiring.
                                        // * Champion debuffs being forced to spec debuffs in `OnStartEffect`.
                                        // * Movement speed debuffs effectiveness decreasing over time.
                                        // This doesn't work will pulsing charm spells, and it's probably safer to exclude every pulsing spell for now.
                                        // This should also ignore effects currently disabled, or being reenabled.
                                        // `IsDisabled` is set to false before this is called, so both need to be checked.
                                        if ((!existingEffect.IsDisabled && !effect.IsEnabling && !newSpell.IsPulsing) ||
                                            existingSpell.SpellType is eSpellType.SpeedDecrease or eSpellType.UnbreakableSpeedDecrease)
                                        {
                                            existingEffect.IsSilent = true;
                                            effect.IsSilent = true;
                                            existingEffect.Stop();
                                            existingGameEffects.Add(effect);
                                            result = AddEffectResult.Added;
                                        }
                                        else
                                        {
                                            effect.PreviousPosition = GetAllEffects().IndexOf(existingEffect);
                                            existingGameEffects[i] = effect;
                                            result = existingEffect.IsActive ? AddEffectResult.RenewedActive : AddEffectResult.RenewedDisabled;
                                        }

                                        Effects.TryAdd(effect.EffectType, existingGameEffects);
                                        _effectIdToEffect[effect.Icon] = effect;
                                        return result;
                                    }
                                }
                            }

                            return AddEffectResult.Added;
                        }
                        else if (effect.EffectType is eEffect.SavageBuff or eEffect.ArmorAbsorptionBuff)
                        {
                            // Player doesn't have this buff yet.
                            if (!existingGameEffects.Where(e => e.SpellHandler.Spell.SpellType == effect.SpellHandler.Spell.SpellType).Any())
                            {
                                Effects.TryAdd(effect.EffectType, [effect]);
                                _effectIdToEffect[effect.Icon] = effect;
                                return AddEffectResult.Added;
                            }
                            else
                                return AddEffectResult.Failed;
                        }
                        else
                        {
                            AddEffectResult result = AddEffectResult.Failed;
                            // foundIsOverwritableEffect is a bool for if we find an overwritable effect when looping over existing effects. Will be used to later to add effects that are not in same effect group.
                            bool foundIsOverwritableEffect = false;

                            for (int i = 0; i < existingGameEffects.Count; i++)
                            {
                                ECSGameEffect existingEffect = existingGameEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // Check if existingEffect is overwritable by new effect.
                                if (existingSpellHandler.IsOverwritable(effect) || effect.EffectType is eEffect.MovementSpeedDebuff)
                                {
                                    foundIsOverwritableEffect = true;

                                    if (effect.EffectType is eEffect.Bladeturn)
                                    {
                                        // PBT should only replace itself.
                                        if (!newSpell.IsPulsing)
                                        {
                                            // Self cast Bladeturns should never be overwritten.
                                            if (existingSpell.Target is not eSpellTarget.SELF)
                                            {
                                                existingEffect.Stop();
                                                result = AddEffectResult.Added;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        if (existingEffect.IsDisabled)
                                            continue;

                                        // Special handling for ablative effects.
                                        // We use the remaining amount instead of the spell value. They also can't be added as disabled effects.
                                        // Note: This ignores subclasses of 'AblativeArmorSpellHandler', so right know we only allow one ablative buff regardless of its type.
                                        if (effect.EffectType is eEffect.AblativeArmor && existingEffect is AblativeArmorECSGameEffect existingAblativeEffect)
                                        {
                                            // 'Damage' represents the absorption% per hit.
                                            if (newSpell.Value * AblativeArmorSpellHandler.ValidateSpellDamage((int) newSpell.Damage) >
                                                existingAblativeEffect.RemainingValue * AblativeArmorSpellHandler.ValidateSpellDamage((int) existingSpell.Damage))
                                            {
                                                existingEffect.Stop();
                                                result = AddEffectResult.Added;
                                            }

                                            break;
                                        }

                                        if (effect.IsBetterThan(existingEffect))
                                        {
                                            // New effect is better than the current effect. Disable or stop the current effect.
                                            if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster
                                                || newSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects
                                                || existingSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects))
                                                existingEffect.Disable();
                                            else
                                                existingEffect.Stop();

                                            result = AddEffectResult.Added;
                                            break;
                                        }
                                        else
                                        {
                                            // New effect is not as good as the current effect, but it can be added in a disabled state.
                                            if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster
                                                || newSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects
                                                || existingSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects))
                                            {
                                                result = AddEffectResult.Disabled;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            // No overwritable effects found that match new spell effect, so add it.
                            if (!foundIsOverwritableEffect)
                                result = AddEffectResult.Added;

                            if (result is AddEffectResult.Failed)
                                return result;

                            if (!HandleConcentration(effect as ECSGameSpellEffect))
                                result = AddEffectResult.Failed;

                            existingGameEffects.Add(effect);
                            Effects.TryAdd(effect.EffectType, existingGameEffects);

                            if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                                _effectIdToEffect[effect.Icon] = effect;

                            return result;
                        }
                    }
                    else if (Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                    {
                        if (!HandleConcentration(effect as ECSGameSpellEffect))
                            return AddEffectResult.Failed;

                        existingEffects.Add(effect);
                        return AddEffectResult.Added;
                    }
                    else
                    {
                        if (!HandleConcentration(effect as ECSGameSpellEffect))
                            return AddEffectResult.Failed;

                        Effects.TryAdd(effect.EffectType, [effect]);

                        if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                            _effectIdToEffect[effect.Icon] = effect;

                        return AddEffectResult.Added;
                    }
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed adding an effect to {effect.Owner}'s effect list", e);

                    return AddEffectResult.Failed;
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

                    EffectListComponent casterEffectListComponent = caster.effectListComponent;
                    casterEffectListComponent.AddUsedConcentration(spellHandler.Spell.Concentration);

                    lock (casterEffectListComponent.ConcentrationEffectsLock)
                    {
                        casterEffectListComponent.ConcentrationEffects.Add(spellEffect);
                    }

                    casterEffectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
                    return true;
                }
            }

            void OnEffectAdded(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectService.GetPlayerUpdateFromEffect(effect.EffectType));

                    if (effect is not ECSGameSpellEffect spellEffect)
                        return;

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
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect added to {effect.Owner}'s effect list", e);
                }
            }

            void OnEffectNotAdded(ECSGameEffect effect)
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

        public RemoveEffectResult RemoveEffect(ECSGameEffect effect)
        {
            RemoveEffectResult result;

            lock (EffectsLock)
            {
                result = RemoveEffectInternal(effect);
            }

            if (result is not RemoveEffectResult.Failed)
                OnEffectRemoved(effect);

            return result;

            RemoveEffectResult RemoveEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    if (!Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
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
                            Effects.Remove(effect.EffectType);

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

                    EffectListComponent casterEffectListComponent = caster.effectListComponent;
                    casterEffectListComponent.AddUsedConcentration(-spellHandler.Spell.Concentration);

                    lock (casterEffectListComponent.ConcentrationEffectsLock)
                    {
                        casterEffectListComponent.ConcentrationEffects.Remove(spellEffect);
                    }

                    casterEffectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
                }
            }

            void OnEffectRemoved(ECSGameEffect effect)
            {
                try
                {
                    RequestPlayerUpdate(EffectService.GetPlayerUpdateFromEffect(effect.EffectType));
                    effect.TryApplyImmunity();
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed processing an effect removed from {effect.Owner}'s effect list", e);
                }
            }
        }

        public void AddUsedConcentration(int amount)
        {
            Interlocked.Add(ref _usedConcentration, amount);
        }

        public List<ECSGameEffect> GetAllEffects()
        {
            lock (EffectsLock)
            {
                var temp = new List<ECSGameEffect>();
                foreach (var effects in Effects.Values.ToList())
                {
                    for (int j = 0; j < effects?.Count; j++)
                    {
                        if (effects[j].EffectType != eEffect.Pulse)
                            temp.Add(effects[j]);
                    }
                }

                return temp.OrderBy(e => e.StartTick).ToList();
            }
        }

        public List<ECSPulseEffect> GetAllPulseEffects()
        {
            lock (EffectsLock)
            {
                var temp = new List<ECSPulseEffect>();
                foreach (var effects in Effects.Values.ToList())
                {
                    for (int j = 0; j < effects?.Count; j++)
                    {
                        if (effects[j].EffectType == eEffect.Pulse)
                            temp.Add((ECSPulseEffect)effects[j]);
                    }
                }

                return temp;
            }
        }

        public ECSGameSpellEffect GetBestDisabledSpellEffect(eEffect effectType = eEffect.Unknown)
        {
            lock (EffectsLock)
            {
                return GetSpellEffects(effectType)?.Where(x => x.IsDisabled).OrderByDescending(x => x.SpellHandler.Spell.Value).FirstOrDefault();
            }
        }

        public List<ECSGameSpellEffect> GetSpellEffects(eEffect effectType = eEffect.Unknown)
        {
            lock (EffectsLock)
            {
                var temp = new List<ECSGameSpellEffect>();
                foreach (var effects in Effects.Values.ToList())
                {
                    for (int j = 0; j < effects?.Count; j++)
                    {
                        if (effects[j] is ECSGameSpellEffect)
                        {
                            if (effectType != eEffect.Unknown)
                            {
                                if (effects[j].EffectType == effectType)
                                    temp.Add(effects[j] as ECSGameSpellEffect);
                            }
                            else
                                temp.Add(effects[j] as ECSGameSpellEffect);
                        }
                    }
                }

                return temp.OrderBy(e => e.StartTick).ToList();
            }
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects()
        {
            lock (EffectsLock)
            {
                var temp = new List<ECSGameAbilityEffect>();
                foreach (var effects in Effects.Values.ToList())
                {
                    for (int j = 0; j < effects?.Count; j++)
                    {
                        if (effects[j] is ECSGameAbilityEffect)
                            temp.Add(effects[j] as ECSGameAbilityEffect);
                    }
                }

                return temp.OrderBy(e => e.StartTick).ToList();
            }
        }

        public ECSGameEffect TryGetEffectFromEffectId(int effectId)
        {
            _effectIdToEffect.TryGetValue(effectId, out var effect);
            return effect;
        }

        public ref int GetLastUpdateEffectsCount()
        {
            return ref _lastUpdateEffectsCount;
        }

        public bool ContainsEffectForEffectType(eEffect effectType)
        {
            try
            {
                return Effects.ContainsKey(effectType);
            }
            catch
            {
                return false;
            }
        }

        public void CancelAll()
        {
            foreach (List<ECSGameEffect> list in Effects.Values.ToList())
            {
                for (int i = list.Count - 1; i >= 0; i--)
                    list[i].Stop();
            }
        }

        public void RequestPlayerUpdate(EffectService.PlayerUpdate playerUpdate)
        {
            lock (_playerUpdatesLock)
            {
                _requestedPlayerUpdates |= playerUpdate;
            }
        }

        public void SendPlayerUpdates()
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
                        playerOwner.Out.SendUpdateIcons(GetAllEffects(), ref GetLastUpdateEffectsCount());
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

        private static void TickEffect(ECSGameEffect effect)
        {
            if (effect is ECSGameAbilityEffect abilityEffect)
                TickAbilityEffect(abilityEffect);
            else if (effect is ECSGameSpellEffect spellEffect)
                TickSpellEffect(spellEffect);

            static void TickAbilityEffect(ECSGameAbilityEffect abilityEffect)
            {
                if (abilityEffect.NextTick != 0 && ServiceUtils.ShouldTickAdjust(ref abilityEffect.NextTick))
                {
                    abilityEffect.OnEffectPulse();
                    abilityEffect.NextTick += abilityEffect.PulseFreq;
                }

                if (abilityEffect.Duration > 0 && ServiceUtils.ShouldTick(abilityEffect.ExpireTick))
                    abilityEffect.Stop();
            }

            static void TickSpellEffect(ECSGameSpellEffect spellEffect)
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
                    if (!spell.IsPulsing || spellHandler.PulseEffect?.IsActive != true)
                        spellEffect.Stop();
                }

                // Make sure the effect actually has a next tick scheduled since some spells are marked as pulsing but actually don't.
                if (spellEffect.IsAllowedToPulse)
                    TickPulsingEffect(spellEffect, spell, spellHandler, caster);

                static void TickConcentrationEffect(ECSGameSpellEffect spellEffect)
                {
                    if (!ServiceUtils.ShouldTickAdjust(ref spellEffect.NextTick))
                        return;

                    ISpellHandler spellHandler = spellEffect.SpellHandler;
                    GameLiving caster = spellHandler.Caster;
                    GameLiving effectOwner = spellEffect.Owner;

                    int radiusToCheck = spellHandler.Spell.SpellType is not eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500;
                    bool isWithinRadius = effectOwner == caster || caster.IsWithinRadius(effectOwner, radiusToCheck);

                    // Check if player is too far away from Caster for Concentration buff, or back in range.
                    if (!isWithinRadius)
                    {
                        if (spellEffect.IsActive)
                        {
                            ECSGameSpellEffect disabled = null;

                            if (effectOwner.effectListComponent.GetSpellEffects(spellEffect.EffectType).Count > 1)
                                disabled = effectOwner.effectListComponent.GetBestDisabledSpellEffect(spellEffect.EffectType);

                            spellEffect.Disable();

                            if (disabled != null)
                                disabled.Enable();
                        }
                    }
                    else if (spellEffect.IsDisabled)
                    {
                        //Check if this effect is better than currently enabled effects. Enable this effect and disable other effect if true.
                        ECSGameSpellEffect enabled = null;
                        effectOwner.effectListComponent.Effects.TryGetValue(spellEffect.EffectType, out List<ECSGameEffect> sameEffectTypeEffects);
                        bool isBest = false;

                        if (sameEffectTypeEffects.Count == 1)
                            isBest = true;
                        else if (sameEffectTypeEffects.Count > 1)
                        {
                            foreach (var tmpEff in sameEffectTypeEffects)
                            {
                                if (tmpEff is ECSGameSpellEffect eff)
                                {
                                    //Check only against enabled spells
                                    if (eff.IsActive)
                                    {
                                        enabled = eff;
                                        isBest = spellHandler.Spell.Value > eff.SpellHandler.Spell.Value;
                                    }
                                }
                            }
                        }

                        if (isBest)
                        {
                            spellEffect.Enable();
                            enabled?.Disable();
                        }
                    }

                    spellEffect.NextTick += spellEffect.PulseFreq;
                }

                static void TickPulsingEffect(ECSGameSpellEffect spellEffect, Spell spell, ISpellHandler spellHandler, GameLiving caster)
                {
                    if (!ServiceUtils.ShouldTickAdjust(ref spellEffect.NextTick))
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

                                if (ServiceUtils.ShouldTickNoEarly(childEffect.ExpireTick))
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
                    else if (spellEffect is not ECSImmunityEffect && spellEffect.EffectType is not eEffect.Pulse && spellEffect.SpellHandler.Spell.SpellType is eSpellType.SpeedDecrease or eSpellType.StyleSpeedDecrease)
                    {
                        double factor = 2.0 - (spellEffect.Duration - spellEffect.GetRemainingTimeForClient()) / (spellEffect.Duration * 0.5);

                        if (factor < 0)
                            factor = 0;
                        else if (factor > 1)
                            factor = 1;

                        spellEffect.Owner.BuffBonusMultCategory1.Set((int) eProperty.MaxSpeed, spellEffect.EffectType, 1.0 - spellEffect.SpellHandler.Spell.Value * factor * 0.01);
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
