using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.AI.Brain;
using DOL.GS.Spells;

namespace DOL.GS
{
    // Component for holding persistent effects on the player.
    public class EffectListComponent : IManagedEntity
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private int _lastUpdateEffectsCount;
        private int _usedConcentration;
        private Dictionary<int, ECSGameEffect> _effectIdToEffect = [];

        public GameLiving Owner { get; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.EffectListComponent);
        public Dictionary<eEffect, List<ECSGameEffect>> Effects { get; } = [];
        public readonly Lock EffectsLock = new();
        public List<ECSGameSpellEffect> ConcentrationEffects { get; } = new List<ECSGameSpellEffect>(20);
        public readonly Lock ConcentrationEffectsLock = new();
        public EffectService.PlayerUpdate RequestedPlayerUpdates { get; private set; }
        public int UsedConcentration => Interlocked.CompareExchange(ref _usedConcentration, 0, 0);

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public bool AddEffect(ECSGameEffect effect)
        {
            lock (EffectsLock)
            {
                if (AddEffectInternal(effect))
                {
                    OnEffectAdded(effect);
                    return true;
                }
            }

            OnEffectNotAdded(effect);
            return false;

            bool AddEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    // Dead owners don't get effects
                    if (!Owner.IsAlive)
                        return false;

                    EntityManager.Add(this);

                    if (effect is ECSGameAbilityEffect)
                    {
                        if (Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                            existingEffects.Add(effect);
                        else
                            Effects.Add(effect.EffectType, [effect]);

                        _effectIdToEffect.TryAdd(effect.Icon, effect);
                        return true;
                    }

                    if (effect is ECSGameSpellEffect newSpellEffect && Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingGameEffects))
                    {
                        ISpellHandler newSpellHandler = newSpellEffect.SpellHandler;
                        Spell newSpell = newSpellHandler.Spell;

                        // RAs use spells with an ID of 0. Differentiating them is tricky and requires some rewriting.
                        // So for now let's prevent overwriting / coexistence altogether.
                        if (newSpell.ID == 0)
                            return false;

                        List<ECSGameSpellEffect> existingEffects = existingGameEffects.Cast<ECSGameSpellEffect>().ToList();

                        // Effects contains this effect already so refresh it
                        if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)) != null)
                        {
                            if (newSpellEffect.IsConcentrationEffect() && !newSpellEffect.RenewEffect)
                                return false;

                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameSpellEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                if ((existingSpell.IsPulsing && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType) && existingSpell.ID == newSpell.ID)
                                    || (existingSpell.IsConcentration && existingEffect == newSpellEffect)
                                    || existingSpell.ID == newSpell.ID)
                                {
                                    if (newSpell.IsPoisonEffect && newSpellEffect.EffectType is eEffect.DamageOverTime)
                                    {
                                        existingEffect.ExpireTick = newSpellEffect.ExpireTick;
                                        newSpellEffect.IsBuffActive = true; // Why?
                                    }
                                    else
                                    {
                                        if (newSpell.IsPulsing)
                                            newSpellEffect.RenewEffect = true;

                                        // Always stop the current effect and let the new one be started by 'EffectService.HandlePropertyModification()'.
                                        // Not calling `OnStopEffect` on the current effect and `OnStartEffect` on the new effect means some initialization may not be performed.
                                        // To give some examples:
                                        // * Effectiveness changes from resurrection illness expiring.
                                        // * Champion debuffs being forced to spec debuffs in `OnStartEffect`.
                                        // * Movement speed debuffs effectiveness decreasing over time.
                                        // This doesn't work will pulsing charm spells, and it's probably safer to exclude every pulsing spell for now.
                                        // This should also ignores effects being reenabled.
                                        if (!newSpellEffect.RenewEffect)
                                        {
                                            existingEffect.IsSilent = true;
                                            existingEffect.OnStopEffect();
                                            newSpellEffect.IsSilent = true;
                                        }

                                        newSpellEffect.IsDisabled = existingEffect.IsDisabled;
                                        newSpellEffect.IsBuffActive = existingEffect.IsBuffActive;
                                        newSpellEffect.PreviousPosition = GetAllEffects().IndexOf(existingEffect);
                                        Effects[newSpellEffect.EffectType][i] = newSpellEffect;
                                        _effectIdToEffect[newSpellEffect.Icon] = newSpellEffect;
                                    }

                                    return true;
                                }
                            }
                        }
                        else if (effect.EffectType is eEffect.SavageBuff or eEffect.ArmorAbsorptionBuff)
                        {
                            if (effect.EffectType is eEffect.ArmorAbsorptionBuff)
                            {
                                for (int i = 0; i < existingEffects.Count; i++)
                                {
                                    // Better Effect so disable the current Effect.
                                    if (newSpellEffect.SpellHandler.Spell.Value >
                                        existingEffects[i].SpellHandler.Spell.Value)
                                    {
                                        EffectService.RequestDisableEffect(existingEffects[i]);
                                        Effects[newSpellEffect.EffectType].Add(newSpellEffect);
                                        _effectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                            // Player doesn't have this buff yet.
                            else if (!existingEffects.Where(e => e.SpellHandler.Spell.SpellType == newSpellEffect.SpellHandler.Spell.SpellType).Any())
                            {
                                Effects[newSpellEffect.EffectType].Add(newSpellEffect);
                                _effectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                        {
                            bool addEffect = false;
                            // foundIsOverwritableEffect is a bool for if we find an overwritable effect when looping over existing effects. Will be used to later to add effects that are not in same effect group.
                            bool foundIsOverwritableEffect = false;

                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameSpellEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // Check if existingEffect is overwritable by new effect.
                                if (existingSpellHandler.IsOverwritable(newSpellEffect) || newSpellEffect.EffectType is eEffect.MovementSpeedDebuff)
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
                                                EffectService.RequestImmediateCancelEffect(existingEffect);
                                                addEffect = true;
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
                                        if (effect.EffectType is eEffect.AblativeArmor &&
                                            existingEffect is AblativeArmorECSGameEffect existingAblativeEffect)
                                        {
                                            // 'Damage' represents the absorption% per hit.
                                            if (newSpell.Value * AblativeArmorSpellHandler.ValidateSpellDamage((int)newSpell.Damage) >
                                                existingAblativeEffect.RemainingValue * AblativeArmorSpellHandler.ValidateSpellDamage((int) existingSpell.Damage))
                                            {
                                                EffectService.RequestImmediateCancelEffect(existingEffect);
                                                addEffect = true;
                                            }

                                            break;
                                        }

                                        if (newSpellEffect.IsBetterThan(existingEffect))
                                        {
                                            // New effect is better than the current effect. Disable or cancel the current effect.
                                            if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster
                                                || newSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects
                                                || existingSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects))
                                                EffectService.RequestDisableEffect(existingEffect);
                                            else
                                                EffectService.RequestImmediateCancelEffect(existingEffect);

                                            addEffect = true;
                                            break;
                                        }
                                        else
                                        {
                                            // New effect is not as good as the current effect, but it can be added in a disabled state.
                                            if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster
                                                || newSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects
                                                || existingSpellHandler.SpellLine.KeyName is GlobalSpellsLines.Potions_Effects))
                                            {
                                                addEffect = true;
                                                newSpellEffect.IsDisabled = true;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }

                            // No overwriteable effects found that match new spell effect, so add it!
                            if (!foundIsOverwritableEffect)
                                addEffect = true;

                            if (addEffect)
                            {
                                existingGameEffects.Add(newSpellEffect);
                                Effects.TryAdd(newSpellEffect.EffectType, existingGameEffects); // The effect type might have been removed by `RequestImmediateCancelEffect`.

                                if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                                    _effectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);

                                return true;
                            }
                        }

                        return false;
                    }
                    else if (Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                        existingEffects.Add(effect);
                    else
                    {
                        Effects.Add(effect.EffectType, [effect]);

                        if (effect.EffectType is not eEffect.Pulse && effect.Icon != 0)
                            _effectIdToEffect.TryAdd(effect.Icon, effect);
                    }

                    return true;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed adding an effect to {effect.Owner}'s effect list", e);

                    return false;
                }
            }

            void OnEffectAdded(ECSGameEffect effect)
            {
                try
                {
                    RequestedPlayerUpdates |= EffectService.GetPlayerUpdateFromEffect(effect.EffectType);

                    if (effect is not ECSGameSpellEffect spellEffect)
                    {
                        effect.OnStartEffect();
                        return;
                    }

                    ISpellHandler spellHandler = spellEffect.SpellHandler;
                    Spell spell = spellHandler?.Spell;
                    GameLiving caster = spellHandler?.Caster;

                    // Update the Concentration List if Conc Buff/Song/Chant.
                    if (spellEffect.ShouldBeAddedToConcentrationList() && !spellEffect.RenewEffect && caster != null)
                    {
                        EffectListComponent casterEffectListComponent = caster.effectListComponent;
                        casterEffectListComponent.AddUsedConcentration(spell.Concentration);

                        lock (casterEffectListComponent.ConcentrationEffectsLock)
                        {
                            casterEffectListComponent.ConcentrationEffects.Add(spellEffect);
                        }

                        casterEffectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
                    }

                    if ((!spellEffect.IsBuffActive && !spellEffect.IsDisabled)
                        || spellEffect is SavageBuffECSGameEffect)
                    {
                        //if (spellEffect.EffectType == eEffect.EnduranceRegenBuff)
                        //{
                        //    var handler = spellHandler as EnduranceRegenSpellHandler;
                        //    ApplyBonus(spellEffect.Owner, handler.BonusCategory1, handler.Property1, spell.Value, 1, false);
                        //}

                        effect.OnStartEffect();
                        effect.IsBuffActive = true;
                    }

                    if (spell.IsPulsing)
                    {
                        // This should allow the caster to see the effect of the first tick of a beneficial pulse effect, even when recasted before the existing effect expired.
                        // It means they can spam some spells, but I consider it a good feedback for the player (example: Paladin's endurance chant).
                        // It should also allow harmful effects to be played on the targets, but not the caster (example: Reaver's PBAEs -- the flames, not the waves).
                        // It should prevent double animations too (only checking 'IsHarmful' and 'RenewEffect' would make resist chants play twice).
                        if (spellEffect is ECSPulseEffect)
                        {
                            if (!spell.IsHarmful && spell.SpellType is not eSpellType.Charm && !spellEffect.RenewEffect)
                                EffectService.SendSpellAnimation(spellEffect);
                        }
                        else if (spell.IsHarmful)
                            EffectService.SendSpellAnimation(spellEffect);
                    }
                    else if (spellEffect is not ECSImmunityEffect)
                        EffectService.SendSpellAnimation(spellEffect);
                    if (effect is StatDebuffECSEffect && spell.CastTime == 0)
                        StatDebuffECSEffect.TryDebuffInterrupt(spell, effect.OwnerPlayer, caster);
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

        public bool RemoveEffect(ECSGameEffect effect)
        {
            lock (EffectsLock)
            {
                if (RemoveEffectInternal(effect))
                {
                    OnEffectRemoved(effect);
                    return true;
                }
            }

            return false;

            bool RemoveEffectInternal(ECSGameEffect effect)
            {
                try
                {
                    if (!Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingEffects))
                        return false;

                    if (effect.CancelEffect)
                    {
                        // Get the effectToRemove from the Effects list. Had issues trying to remove the effect directly from the list if it wasn't the same object.
                        ECSGameEffect effectToRemove = existingEffects.FirstOrDefault(e => e.Name == effect.Name);
                        existingEffects.Remove(effectToRemove);
                        _effectIdToEffect.Remove(effect.Icon);

                        if (existingEffects.Count == 0)
                        {
                            _effectIdToEffect.Remove(effect.Icon);
                            Effects.Remove(effect.EffectType);
                        }
                    }

                    return true;
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed removing an effect from {effect.Owner}'s effect list", e);

                    return false;
                }
            }

            void OnEffectRemoved(ECSGameEffect effect)
            {
                try
                {
                    RequestedPlayerUpdates |= EffectService.GetPlayerUpdateFromEffect(effect.EffectType);

                    if (effect is not ECSGameSpellEffect spellEffect)
                        effect.OnStopEffect();
                    else
                    {
                        if (spellEffect.IsBuffActive && spellEffect is not ECSImmunityEffect)
                            effect.OnStopEffect();

                        effect.IsBuffActive = false;
                        GameLiving caster = effect.SpellHandler.Caster;

                        // Update the Concentration List if Conc Buff/Song/Chant.
                        if (caster != null && effect.CancelEffect && effect.ShouldBeRemovedFromConcentrationList())
                        {
                            EffectListComponent casterEffectListComponent = caster.effectListComponent;
                            casterEffectListComponent.AddUsedConcentration(-spellEffect.SpellHandler.Spell.Concentration);

                            lock (casterEffectListComponent.ConcentrationEffectsLock)
                            {
                                casterEffectListComponent.ConcentrationEffects.Remove(spellEffect);
                            }

                            casterEffectListComponent.RequestPlayerUpdate(EffectService.PlayerUpdate.CONCENTRATION);
                        }
                    }

                    effect.TryApplyImmunity();

                    if (effect.IsDisabled)
                        return;

                    List<ECSGameSpellEffect> spellEffects = GetSpellEffects(effect.EffectType);

                    if (spellEffects.Count <= 0)
                        return;

                    ECSGameSpellEffect enableEffect = spellEffects.OrderByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();

                    if (enableEffect != null && enableEffect.IsDisabled)
                        EffectService.RequestEnableEffect(enableEffect);
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
            lock (EffectsLock)
            {
                foreach (List<ECSGameEffect> list in Effects.Values.ToList())
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                        EffectService.RequestImmediateCancelEffect(list[i]);
                }
            }
        }

        public void RequestPlayerUpdate(EffectService.PlayerUpdate playerUpdate)
        {
            lock (EffectsLock)
            {
                RequestedPlayerUpdates |= playerUpdate;
            }

            // Forces an update in case our effect list component isn't ticking.
            // Don't call `SendPlayerUpdates` directly.
            EntityManager.Add(this);
        }

        public void SendPlayerUpdates()
        {
            if (RequestedPlayerUpdates is EffectService.PlayerUpdate.NONE)
                return;

            lock (EffectsLock)
            {
                if (Owner is GamePlayer playerOwner)
                {
                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.ICONS) != 0)
                    {
                        playerOwner.Group?.UpdateMember(playerOwner, true, false);
                        playerOwner.Out.SendUpdateIcons(GetAllEffects(), ref GetLastUpdateEffectsCount());
                    }

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.STATUS) != 0)
                        playerOwner.Out.SendStatusUpdate();

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.STATS) != 0)
                        playerOwner.Out.SendCharStatsUpdate();

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.RESISTS) != 0)
                        playerOwner.Out.SendCharResistsUpdate();

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.WEAPON_ARMOR) != 0)
                        playerOwner.Out.SendUpdateWeaponAndArmorStats();

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.ENCUMBERANCE) != 0)
                        playerOwner.UpdateEncumbrance();

                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.CONCENTRATION) != 0)
                        playerOwner.Out.SendConcentrationList();
                }
                else if (Owner is GameNPC npcOwner && npcOwner.Brain is IControlledBrain npcOwnerBrain)
                {
                    if ((RequestedPlayerUpdates & EffectService.PlayerUpdate.ICONS) != 0)
                        npcOwnerBrain.UpdatePetWindow();
                }

                RequestedPlayerUpdates = EffectService.PlayerUpdate.NONE;
            }
        }
    }
}
