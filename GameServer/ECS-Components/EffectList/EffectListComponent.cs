using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Effects;
using DOL.GS.Spells;

namespace DOL.GS
{
    //Component for holding persistent Effects on the player//
    
    public class EffectListComponent
    {
        private int _lastUpdateEffectsCount;

        public GameLiving Owner { get; private set; }
        public Dictionary<eEffect, List<ECSGameEffect>> Effects { get; private set; } = new Dictionary<eEffect, List<ECSGameEffect>>();
        public object EffectsLock { get; private set; } = new();
        public List<ECSGameSpellEffect> ConcentrationEffects { get; private set; } = new List<ECSGameSpellEffect>(20);
        public object ConcentrationEffectsLock { get; private set; } = new();

        private readonly Dictionary<int, ECSGameEffect> EffectIdToEffect = new();

        public EffectListComponent(GameLiving owner)
        {
            Owner = owner;
        }

        public bool AddEffect(ECSGameEffect effect)
        {
            lock (EffectsLock)
            {
                try
                {
                    // Dead owners don't get effects
                    if (!Owner.IsAlive || Owner.ObjectState != GameObject.eObjectState.Active)
                        return false;

                    if (!EntityManager.GetLivingByComponent(typeof(EffectListComponent)).ToArray().Contains(Owner))
                        EntityManager.AddComponent(typeof(EffectListComponent), Owner);

                    // Check to prevent crash from holding sprint button down.
                    if (effect is ECSGameAbilityEffect)
                    {
                        if (!Effects.ContainsKey(effect.EffectType))
                            Effects.Add(effect.EffectType, new List<ECSGameEffect> { effect });
                        else if (effect.EffectType is eEffect.Protect or eEffect.Guard)
                            Effects[effect.EffectType].Add(effect);
                        return true;
                    }

                    if (effect is ECSGameSpellEffect newSpellEffect && Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingGameEffects))
                    {
                        ISpellHandler newSpellHandler = newSpellEffect.SpellHandler;
                        Spell newSpell = newSpellHandler.Spell;
                        List<ECSGameSpellEffect> existingEffects = existingGameEffects.Cast<ECSGameSpellEffect>().ToList();

                        // Effects contains this effect already so refresh it
                        if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == newSpell.ID || (newSpell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == newSpell.EffectGroup && newSpell.IsPoisonEffect)) != null)
                        {
                            if (newSpellEffect.IsConcentrationEffect() && !newSpellEffect.RenewEffect)
                                return false;
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameSpellEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffects[i].SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // Console.WriteLine($"Effect found! Name: {existingEffect.Name}, Effectiveness: {existingEffect.Effectiveness}, Poison: {existingSpell.IsPoisonEffect}, ID: {existingSpell.ID}, EffectGroup: {existingSpell.EffectGroup}");

                                if ((existingSpell.IsPulsing && newSpellHandler.Caster.LastPulseCast == newSpell && existingSpell.ID == newSpell.ID)
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

                                        // Stopping the effect allows the spell to update its effectiveness (i.e. after resurrection illness).
                                        // Also needed for movement speed debuffs' since their effect decrease over time.
                                        // OnStartEffect() should be called in EffectService.HandlePropertyModification()
                                        if (existingEffect.Effectiveness != newSpellEffect.Effectiveness
                                            || existingSpell.SpellType is (byte)eSpellType.SpeedDecrease or (byte)eSpellType.UnbreakableSpeedDecrease)
                                            existingEffect.OnStopEffect();

                                        newSpellEffect.IsDisabled = existingEffect.IsDisabled;
                                        newSpellEffect.IsBuffActive = existingEffect.IsBuffActive;
                                        newSpellEffect.PreviousPosition = GetAllEffects().IndexOf(existingEffect);
                                        Effects[newSpellEffect.EffectType][i] = newSpellEffect;
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
                                    // Better Effect so disable the current Effect
                                    if (newSpellEffect.SpellHandler.Spell.Value >
                                        existingEffects[i].SpellHandler.Spell.Value)
                                    {
                                        EffectService.RequestDisableEffect(existingEffects[i]);
                                        Effects[newSpellEffect.EffectType].Add(newSpellEffect);
                                        EffectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                                }
                            }
                            // Player doesn't have this buff yet
                            else if (!existingEffects.Where(e => e.SpellHandler.Spell.SpellType == newSpellEffect.SpellHandler.Spell.SpellType).Any())
                            {
                                Effects[newSpellEffect.EffectType].Add(newSpellEffect);
                                EffectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
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
                            // Check to see if we can add new Effect
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                ECSGameSpellEffect existingEffect = existingEffects[i];
                                ISpellHandler existingSpellHandler = existingEffects[i].SpellHandler;
                                Spell existingSpell = existingSpellHandler.Spell;

                                // Check if existingEffect is overwritable by new effect
                                if (existingSpellHandler.IsOverwritable(newSpellEffect) || newSpellEffect.EffectType == eEffect.MovementSpeedDebuff)
                                {
                                    foundIsOverwriteableEffect = true;
                                    if (effect.EffectType != eEffect.Bladeturn)
                                    {
                                        // New Effect is better than the current enabled effect so disable the current Effect and add the new effect.
                                        if ((newSpell.Value > existingSpell.Value || newSpell.Damage > existingSpell.Damage)
                                            && !existingEffects[i].IsDisabled)
                                        {
                                            if (newSpell.IsHelpful && (newSpellHandler.Caster != existingSpellHandler.Caster
                                                || newSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects
                                                || existingSpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects))
                                                EffectService.RequestDisableEffect(existingEffects[i]);
                                            else
                                                EffectService.RequestCancelEffect(existingEffects[i]);
                                            addEffect = true;
                                        }
                                        // New Effect is not as good as current effect.
                                        else if (newSpell.Value < existingSpell.Value || newSpell.Damage < existingSpell.Damage)
                                            addEffect = false;
                                    }
                                    else
                                    {
                                        // PBT should only replace itself
                                        if (!newSpell.IsPulsing)
                                        {
                                            // Self cast Bladeturns should never be overwritten
                                            if (existingSpell.Target.ToLower() != "self")
                                            {
                                                EffectService.RequestCancelEffect(existingEffect);
                                                addEffect = true;
                                            }
                                        }
                                    }
                                }
                            }

                            // No overwriteable effects found that match new spell effect, so add it!
                            if (!foundIsOverwriteableEffect)
                                addEffect = true;

                            if (addEffect)
                            {
                                Effects[newSpellEffect.EffectType].Add(newSpellEffect);
                                EffectIdToEffect.TryAdd(newSpellEffect.Icon, newSpellEffect);
                                return true;
                            }
                        }
                        //Console.WriteLine("Effect List contains type: " + effect.EffectType.ToString() + " (" + effect.Owner.Name + ")");
                        return false;
                    }
                    else if (Effects.ContainsKey(effect.EffectType))
                    {
                        Effects[effect.EffectType].Add(effect);
                    }
                    else
                    {
                        Effects.Add(effect.EffectType, new List<ECSGameEffect> { effect });
                        if (effect.EffectType != eEffect.Pulse && effect.Icon != 0)
                            EffectIdToEffect.Add(effect.Icon, effect);
                    }
                    return true;
                }
                catch
                {
                    //Console.WriteLine($"Error adding an Effect {e}");
                    return false;
                }               
            }
            
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

        public List<IConcentrationEffect> GetConcentrationEffects()
        {
            lock (EffectsLock)
            {
                var temp = new List<IConcentrationEffect>();
                var allEffects = Effects.Values.ToList();

                if (allEffects != null)
                {
                    foreach (var effects in allEffects)
                    {
                        for (int j = 0; j < effects?.Count; j++)
                        {
                            if (effects[j] is ECSPulseEffect || effects[j].IsConcentrationEffect())
                                temp.Add(effects[j] as IConcentrationEffect);
                        }
                    }
                }
                return temp;
            }
        }

        public ECSGameSpellEffect GetBestDisabledSpellEffect(eEffect effectType = eEffect.Unknown)
        {
            lock (EffectsLock)
            {
                return GetSpellEffects(effectType)?.OrderByDescending(e => e.IsDisabled).ThenByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
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
            EffectIdToEffect.TryGetValue(effectId, out var effect);
            return effect;
        }

        public ref int GetLastUpdateEffectsCount()
        {
            return ref _lastUpdateEffectsCount;
        }

        public bool RemoveEffect(ECSGameEffect effect)
        {
            lock (EffectsLock)
            {
                try
                {
                    if (!Effects.ContainsKey(effect.EffectType))
                    {
                        //Console.WriteLine("Effect List does not contain type: " + effect.EffectType.ToString());
                        return false;
                    }
                    else
                    {
                        if (effect.CancelEffect)
                        {
                            List<ECSGameEffect> existingEffects = Effects[effect.EffectType];
                            
                            // Get the effectToRemove from the Effects list. Had issues trying to remove the effect directly from the list if it wasn't the same object.
                            ECSGameEffect effectToRemove = existingEffects.FirstOrDefault(e => e.Name == effect.Name);
                            Effects[effect.EffectType].Remove(effectToRemove);

                            EffectIdToEffect.Remove(effect.Icon);
                            
                            if (Effects[effect.EffectType].Count == 0)
                            {
                                EffectIdToEffect.Remove(effect.Icon);
                                Effects.Remove(effect.EffectType);
                            }

                            if (Effects.Count == 0)
                                EntityManager.RemoveComponent(typeof(EffectListComponent), Owner);
                        }
                        return true;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error removing an Effect from {effect.Owner}'s EffectList {e}");
                    return false;
                }

            }
        }

        public bool ContainsEffectForEffectType(eEffect effectType)
        {
            try
            {
                if (Effects.ContainsKey(effectType))
                    return true;
                else
                    return false;
            } 
            catch
            {
                //Console.WriteLine($"Error attempting to check effect type");
                return false;
            }
        }

        public void CancelAll()
        {
            foreach (var effects in Effects.Values.ToList())
            {
                for (int j = 0; j < effects.Count; j++)
                {
                    EffectService.RequestCancelEffect(effects[j]);
                }
            }
        }

    }
}
