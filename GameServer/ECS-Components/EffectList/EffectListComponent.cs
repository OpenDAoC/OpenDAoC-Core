using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Effects;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;

namespace DOL.GS
{
    // Component for holding persistent effects on the player.
    public class EffectListComponent : IManagedEntity
    {
        public ConcurrentDictionary<eEffect, List<ECSGameEffect>> _effects = new();
        private ConcurrentDictionary<int, ECSGameEffect> _effectIdToEffect = new();
        private ConcurrentQueue<ECSGameEffect> _effectToBeAdded = new();
        private int _lastUpdateEffectsCount;

        public GameLiving Owner { get; private set; }
        public EntityManagerId EntityManagerId { get; set; } = new(EntityManager.EntityType.EffectListComponent, false);
        public List<ECSGameSpellEffect> ConcentrationEffects { get; private set; } = new List<ECSGameSpellEffect>(20);
        public object ConcentrationEffectsLock { get; private set; } = new();

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public void Tick()
        {
            while (_effectToBeAdded.TryDequeue(out ECSGameEffect effect))
                OnAddEffect(effect, AddEffect(effect));

            if (!_effects.IsEmpty)
                CheckEffects();
            else
                EntityManager.Remove(this);
        }

        public void StartAddEffect(ECSGameEffect effect)
        {
            _effectToBeAdded.Enqueue(effect);
            EntityManager.Add(this);
        }

        private bool AddEffect(ECSGameEffect effect)
        {
            // Dead owners don't get effects.
            if (!Owner.IsAlive)
                return false;

            // Check to prevent crash from holding sprint button down.
            if (effect is ECSGameAbilityEffect)
            {
                if (_effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> effects))
                {
                    lock ((effects as ICollection).SyncRoot)
                    {
                        effects.Add(effect);
                    }
                }
                else
                    _effects.TryAdd(effect.EffectType, new List<ECSGameEffect> { effect });

                _effectIdToEffect.TryAdd(effect.Icon, effect);
                return true;
            }

            if (effect is ECSGameSpellEffect newSpellEffect && _effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingGameEffects))
            {
                ISpellHandler newSpellHandler = newSpellEffect.SpellHandler;
                Spell newSpell = newSpellHandler.Spell;

                // RAs use spells with an ID of 0. Differentiating them is tricky and requires some rewriting.
                // So for now let's prevent overwriting / coexistence altogether.
                if (newSpell.ID == 0)
                    return false;

                lock (((ICollection) existingGameEffects).SyncRoot)
                {
                    // Effects contains this effect already so refresh it
                    if (existingGameEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)) != null)
                    {
                        if (newSpellEffect.IsConcentrationEffect() && !newSpellEffect.RenewEffect)
                            return false;
                        for (int i = 0; i < existingGameEffects.Count; i++)
                        {
                            ECSGameEffect existingEffect = existingGameEffects[i];
                            ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                            Spell existingSpell = existingSpellHandler.Spell;

                            // Console.WriteLine($"Effect found! Name: {existingEffect.Name}, Effectiveness: {existingEffect.Effectiveness}, Poison: {existingSpell.IsPoisonEffect}, ID: {existingSpell.ID}, EffectGroup: {existingSpell.EffectGroup}");

                            if ((existingSpell.IsPulsing && effect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(effect.SpellHandler.Spell.SpellType) && existingSpell.ID == newSpell.ID)
                                || (existingSpell.IsConcentration && existingEffect == newSpellEffect)
                                || existingSpell.ID == newSpell.ID)
                            {
                                if (newSpell.IsPoisonEffect && newSpellEffect.EffectType == eEffect.DamageOverTime)
                                {
                                    existingEffect.ExpireTick = newSpellEffect.ExpireTick;
                                    newSpellEffect.IsBuffActive = true; // Why?
                                }
                                else
                                {
                                    if (newSpell.IsPulsing)
                                        newSpellEffect.RenewEffect = true;

                                    // If the effectiveness changed (for example after resurrection illness expired), we need to stop the effect.
                                    // It will be restarted in 'EffectService.HandlePropertyModification()'.
                                    // Also needed for movement speed debuffs' since their effect decrease over time.
                                    if (existingEffect.Effectiveness != newSpellEffect.Effectiveness ||
                                        existingSpell.SpellType is eSpellType.SpeedDecrease or eSpellType.UnbreakableSpeedDecrease)
                                        existingEffect.OnStopEffect();

                                    newSpellEffect.IsDisabled = existingEffect.IsDisabled;
                                    newSpellEffect.IsBuffActive = existingEffect.IsBuffActive;
                                    newSpellEffect.PreviousPosition = GetAllEffects<ECSGameEffect>().IndexOf(existingEffect);
                                    existingGameEffects[i] = newSpellEffect;
                                    _effectIdToEffect.AddOrUpdate(newSpellEffect.Icon, newSpellEffect, (key, oldValue) => newSpellEffect);
                                }

                                return true;
                            }
                        }
                    }
                    else if (effect.EffectType is eEffect.SavageBuff or eEffect.ArmorAbsorptionBuff)
                    {
                        if (effect.EffectType is eEffect.ArmorAbsorptionBuff)
                        {
                            for (int i = 0; i < existingGameEffects.Count; i++)
                            {
                                // Better Effect so disable the current Effect.
                                if (newSpellEffect.SpellHandler.Spell.Value > existingGameEffects[i].SpellHandler.Spell.Value)
                                {
                                    EffectService.RequestDisableEffect(existingGameEffects[i]);
                                    existingGameEffects.Add(newSpellEffect);
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
                        else if (!existingGameEffects.Where(e => e.SpellHandler.Spell.SpellType == newSpellEffect.SpellHandler.Spell.SpellType).Any())
                        {
                            existingGameEffects.Add(newSpellEffect);
                            _effectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
                            return true;
                        }
                        else
                            return false;
                    }
                    else
                    {
                        bool addEffect = false;
                        // foundIsOverwriteableEffect is a bool for if we find an overwriteable effect when looping over existing effects. Will be used to later to add effects that are not in same effect group.
                        bool foundIsOverwriteableEffect = false;

                        for (int i = 0; i < existingGameEffects.Count; i++)
                        {
                            ECSGameEffect existingEffect = existingGameEffects[i];
                            ISpellHandler existingSpellHandler = existingEffect.SpellHandler;
                            Spell existingSpell = existingSpellHandler.Spell;

                            // Check if existingEffect is overwritable by new effect.
                            if (existingSpellHandler.IsOverwritable(newSpellEffect) || newSpellEffect.EffectType == eEffect.MovementSpeedDebuff)
                            {
                                foundIsOverwriteableEffect = true;

                                if (effect.EffectType == eEffect.Bladeturn)
                                {
                                    // PBT should only replace itself.
                                    if (!newSpell.IsPulsing)
                                    {
                                        // Self cast Bladeturns should never be overwritten.
                                        if (existingSpell.Target.ToLower() != "self")
                                        {
                                            EffectService.RequestCancelEffect(existingEffect);
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
                                    if (effect.EffectType == eEffect.AblativeArmor &&
                                        existingEffect is AblativeArmorECSGameEffect existingAblativeEffect)
                                    {
                                        // 'Damage' represents the absorption% per hit.
                                        if (newSpell.Value * AblativeArmorSpellHandler.ValidateSpellDamage((int)newSpell.Damage) >
                                            existingAblativeEffect.RemainingValue *  AblativeArmorSpellHandler.ValidateSpellDamage((int)existingSpell.Damage))
                                        {
                                            EffectService.RequestCancelEffect(existingEffect);
                                            addEffect = true;
                                        }

                                        break;
                                    }

                                    // New Effect is better than the current enabled effect so disable the current Effect and add the new effect.
                                    if (newSpell.Value > existingSpell.Value || newSpell.Damage > existingSpell.Damage)
                                    {
                                        if (newSpell.IsHelpful&& (newSpellHandler.Caster != existingSpellHandler.Caster ||
                                            newSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects ||
                                            existingSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects))
                                        {
                                            EffectService.RequestDisableEffect(existingEffect);
                                        }
                                        else
                                            EffectService.RequestCancelEffect(existingEffect);

                                        addEffect = true;
                                        break;
                                    }
                                    // New Effect is not as good as current effect, but it can be added in a disabled state.
                                    else
                                    {
                                        if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster ||
                                            newSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects ||
                                            existingSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects))
                                        {
                                            addEffect = true;
                                            newSpellEffect.IsDisabled = true;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // No overwritable effects found that match new spell effect, so add it.
                        if (!foundIsOverwriteableEffect)
                            addEffect = true;

                        if (addEffect)
                        {
                            existingGameEffects.Add(newSpellEffect);

                            if (effect.EffectType != eEffect.Pulse && effect.Icon != 0)
                                _effectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);

                            return true;
                        }
                    }
                }

                return false;
            }
            else if (_effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> effects))
            {
                lock ((effects as ICollection).SyncRoot)
                {
                    effects.Add(effect);
                }
            }
            else
            {
                _effects.TryAdd(effect.EffectType, new List<ECSGameEffect> { effect });

                if (effect.EffectType != eEffect.Pulse && effect.Icon != 0)
                    _effectIdToEffect.TryAdd(effect.Icon, effect);
            }

            return true;
        }

        private void OnAddEffect(ECSGameEffect effect, bool success)
        {
            ECSGameSpellEffect spellEffect = effect as ECSGameSpellEffect;

            if (!success)
            {
                if (spellEffect != null && !spellEffect.SpellHandler.Spell.IsPulsing)
                {
                    EffectService.SendSpellResistAnimation(effect as ECSGameSpellEffect);

                    if (spellEffect.SpellHandler.Caster is GameSummonedPet petCaster && petCaster.Owner is GamePlayer casterOwner)
                        ChatUtil.SendResistMessage(casterOwner, "GamePlayer.Caster.Buff.EffectAlreadyActive", effect.Owner.GetName(0, true));

                    if (spellEffect.SpellHandler.Caster is GamePlayer playerCaster)
                        ChatUtil.SendResistMessage(playerCaster, "GamePlayer.Caster.Buff.EffectAlreadyActive", effect.Owner.GetName(0, true));
                }

                return;
            }

            ISpellHandler spellHandler = spellEffect?.SpellHandler;
            Spell spell = spellHandler?.Spell;
            GameLiving caster = spellHandler?.Caster;

            // Update the Concentration List if Conc Buff/Song/Chant.
            if (spellEffect != null && spellEffect.ShouldBeAddedToConcentrationList() && !spellEffect.RenewEffect)
            {
                if (caster != null && ConcentrationEffects != null)
                {
                    caster.UsedConcentration += spell.Concentration;

                    lock (ConcentrationEffectsLock)
                    {
                        ConcentrationEffects.Add(spellEffect);
                    }

                    if (caster is GamePlayer p)
                        p.Out.SendConcentrationList();
                }
            }

            if (spellEffect != null)
            {
                if ((!spellEffect.IsBuffActive && !spellEffect.IsDisabled) || spellEffect is SavageBuffECSGameEffect)
                {
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
                        if (!spell.IsHarmful && spell.SpellType != eSpellType.Charm && !spellEffect.RenewEffect)
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
            else
                effect.OnStartEffect();

            EffectService.UpdateEffectIcons(effect);
        }

        private void CheckEffects()
        {
            foreach (KeyValuePair<eEffect, List<ECSGameEffect>> effectPairs in _effects)
            {
                lock (((ICollection) effectPairs.Value).SyncRoot)
                {
                    foreach  (ECSGameEffect effect in effectPairs.Value)
                    {
                        if (!effect.Owner.IsAlive || effect.Owner.ObjectState == GameObject.eObjectState.Deleted)
                        {
                            EffectService.RequestCancelEffect(effect);
                            continue;
                        }

                        // TEMP - A lot of the code below assumes effects come from spells but many effects come from abilities (Sprint, Stealth, RAs, etc)
                        // This will need a better refactor later but for now this prevents crashing while working on porting over non-spell based effects to our system.
                        if (effect is ECSGameAbilityEffect)
                        {
                            if (effect.NextTick != 0 && GameLoop.GameLoopTime > effect.NextTick)
                                effect.OnEffectPulse();

                            if (effect.Duration > 0 && GameLoop.GameLoopTime > effect.ExpireTick)
                                EffectService.RequestCancelEffect(effect);

                            continue;
                        }
                        else if (effect is ECSGameSpellEffect gameSpellEffect)
                        {
                            if (GameLoop.GameLoopTime > gameSpellEffect.ExpireTick && (!gameSpellEffect.IsConcentrationEffect() || gameSpellEffect.SpellHandler.Spell.IsFocus))
                            {
                                if (gameSpellEffect.EffectType == eEffect.Pulse && gameSpellEffect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(gameSpellEffect.SpellHandler.Spell.SpellType))
                                {
                                    if (gameSpellEffect.SpellHandler.Spell.PulsePower > 0)
                                    {
                                        if (gameSpellEffect.SpellHandler.Caster.Mana >= gameSpellEffect.SpellHandler.Spell.PulsePower)
                                        {
                                            gameSpellEffect.SpellHandler.Caster.Mana -= gameSpellEffect.SpellHandler.Spell.PulsePower;
                                            gameSpellEffect.SpellHandler.StartSpell(null);
                                            gameSpellEffect.ExpireTick += gameSpellEffect.PulseFreq;
                                        }
                                        else
                                        {
                                            ((SpellHandler)gameSpellEffect.SpellHandler).MessageToCaster("You do not have enough power and your spell was canceled.", eChatType.CT_SpellExpires);
                                            EffectService.RequestCancelConcEffect(gameSpellEffect);
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        gameSpellEffect.SpellHandler.StartSpell(null);
                                        gameSpellEffect.ExpireTick += gameSpellEffect.PulseFreq;
                                    }

                                    if (gameSpellEffect.SpellHandler.Spell.IsHarmful && gameSpellEffect.SpellHandler.Spell.SpellType != eSpellType.SpeedDecrease)
                                    {
                                        if (!(gameSpellEffect.Owner.IsMezzed || gameSpellEffect.Owner.IsStunned))
                                            ((SpellHandler)gameSpellEffect.SpellHandler).SendCastAnimation();
                                    }
                                }
                                else
                                {
                                    if (gameSpellEffect.SpellHandler.Spell.IsPulsing && gameSpellEffect.SpellHandler.Caster.ActivePulseSpells.ContainsKey(gameSpellEffect.SpellHandler.Spell.SpellType) &&
                                        gameSpellEffect.ExpireTick >= (gameSpellEffect.LastTick + (gameSpellEffect.Duration > 0 ? gameSpellEffect.Duration : gameSpellEffect.PulseFreq)))
                                    {
                                        //Add time to effect to make sure the spell refreshes instead of cancels
                                        gameSpellEffect.ExpireTick += GameLoop.TICK_RATE;
                                        gameSpellEffect.LastTick = GameLoop.GameLoopTime;
                                    }
                                    else
                                        EffectService.RequestCancelEffect(gameSpellEffect);
                                }
                            }

                            if (gameSpellEffect is not ECSImmunityEffect && gameSpellEffect.EffectType != eEffect.Pulse && gameSpellEffect.SpellHandler.Spell.SpellType == eSpellType.SpeedDecrease)
                            {
                                if (GameLoop.GameLoopTime > gameSpellEffect.NextTick)
                                {
                                    double factor = 2.0 - (gameSpellEffect.Duration - gameSpellEffect.GetRemainingTimeForClient()) / (double)(gameSpellEffect.Duration >> 1);

                                    if (factor < 0)
                                        factor = 0;
                                    else if (factor > 1)
                                        factor = 1;

                                    //effect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, effect.SpellHandler.Spell.ID, 1.0 - effect.SpellHandler.Spell.Value * factor * 0.01);
                                    gameSpellEffect.Owner.BuffBonusMultCategory1.Set((int)eProperty.MaxSpeed, gameSpellEffect.EffectType, 1.0 - gameSpellEffect.SpellHandler.Spell.Value * factor * 0.01);

                                    UnbreakableSpeedDecreaseSpellHandler.SendUpdates(gameSpellEffect.Owner);
                                    gameSpellEffect.NextTick = GameLoop.GameLoopTime + gameSpellEffect.TickInterval;
                                    if (factor <= 0)
                                        gameSpellEffect.ExpireTick = GameLoop.GameLoopTime - 1;
                                }
                            }

                            if (gameSpellEffect.NextTick != 0 && GameLoop.GameLoopTime >= gameSpellEffect.NextTick && GameLoop.GameLoopTime < gameSpellEffect.ExpireTick)
                                gameSpellEffect.OnEffectPulse();

                            if (gameSpellEffect.IsConcentrationEffect() && GameLoop.GameLoopTime > gameSpellEffect.NextTick)
                            {
                                int radiusToCheck = gameSpellEffect.SpellHandler.Spell.SpellType != eSpellType.EnduranceRegenBuff ? ServerProperties.Properties.BUFF_RANGE > 0 ? ServerProperties.Properties.BUFF_RANGE : 5000 : 1500;
                                bool isWithinRadius = gameSpellEffect.SpellHandler.Caster.IsWithinRadius(gameSpellEffect.Owner, radiusToCheck);

                                // Check if player is too far away from Caster for Concentration buff, or back in range.
                                if (!isWithinRadius)
                                {
                                    if (!gameSpellEffect.IsDisabled)
                                    {
                                        ECSGameSpellEffect disabled = null;
                                        if (gameSpellEffect.Owner.effectListComponent.GetSpellEffects(gameSpellEffect.EffectType).Count > 1)
                                            disabled = gameSpellEffect.Owner.effectListComponent.GetBestDisabledSpellEffect(gameSpellEffect.EffectType);

                                        EffectService.RequestDisableEffect(gameSpellEffect);

                                        if (disabled != null)
                                            EffectService.RequestEnableEffect(disabled);
                                    }
                                }
                                else if (gameSpellEffect.IsDisabled)
                                {
                                    //Check if this effect is better than currently enabled effects. Enable this effect and disable other effect if true.
                                    ECSGameSpellEffect enabled = null;
                                    gameSpellEffect.Owner.effectListComponent._effects.TryGetValue(gameSpellEffect.EffectType, out List<ECSGameEffect> sameEffectTypeEffects);
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
                                                if (!eff.IsDisabled)
                                                {
                                                    enabled = eff;
                                                    isBest = gameSpellEffect.SpellHandler.Spell.Value > eff.SpellHandler.Spell.Value;
                                                }
                                            }
                                        }
                                    }

                                    if (isBest)
                                    {
                                        EffectService.RequestEnableEffect(gameSpellEffect);

                                        if (enabled != null)
                                            EffectService.RequestDisableEffect(enabled);
                                    }
                                }

                                gameSpellEffect.NextTick = GameLoop.GameLoopTime + gameSpellEffect.PulseFreq;
                            }
                        }
                    }
                }
            }
        }

        public List<T> GetAllEffects<T>(Predicate<T> predicate = null) where T : ECSGameEffect
        {
            List<T> result = new();

            foreach (List<ECSGameEffect> effects in _effects.Values)
            {
                lock ((effects as ICollection).SyncRoot)
                {
                    for (int i = 0; i < effects.Count; i++)
                    {
                        if (effects[i] is T effect && (predicate == null || predicate(effect)))
                            result.Add(effect);
                    }
                }
            }

            return result;
        }

        public List<ECSGameEffect> GetAllEffects(Predicate<ECSGameEffect> predicate)
        {
            return GetAllEffects(predicate);
        }

        public List<ECSPulseEffect> GetAllPulseEffects(Predicate<ECSPulseEffect> predicate)
        {
            return GetAllEffects(predicate);
        }

        public List<ECSGameEffect> GetConcentrationEffects(Predicate<ECSGameEffect> predicate)
        {
            return GetAllEffects(x => predicate(x) && x is IConcentrationEffect);
        }

        public ECSGameSpellEffect GetBestDisabledSpellEffect(eEffect effectType = eEffect.Unknown)
        {
            return GetAllEffects<ECSGameSpellEffect>(x => x.IsDisabled && x.EffectType == effectType).OrderByDescending(x => x.SpellHandler.Spell.Value).FirstOrDefault();
        }

        public List<ECSGameSpellEffect> GetSpellEffects(Predicate<ECSGameSpellEffect> predicate)
        {
            return GetAllEffects(predicate);
        }

        public List<ECSGameAbilityEffect> GetAbilityEffects(Predicate<ECSGameAbilityEffect> predicate)
        {
            return GetAllEffects(predicate);
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

        public bool RemoveEffect(ECSGameEffect effect)
        {
            if (!effect.CancelEffect)
                return true; // ???

            if (_effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> effects))
            {
                lock ((effects as ICollection).SyncRoot)
                {
                    effects.Remove(effect);
                    _effectIdToEffect.TryRemove(effect.Icon, out _);

                    if (effects.Count == 0)
                        _effects.TryRemove(effect.EffectType, out _);
                }

                return true;
            }

            return false;
        }

        public bool ContainsEffectForEffectType(eEffect effectType)
        {
            return _effects.ContainsKey(effectType);
        }

        public void CancelAll()
        {
            foreach (List<ECSGameEffect> effects in _effects.Values)
            {
                lock ((effects as ICollection).SyncRoot)
                {
                    for (int i = 0; i < effects.Count; i++)
                        EffectService.RequestCancelEffect(effects[i]);
                }
            }
        }
    }
}
