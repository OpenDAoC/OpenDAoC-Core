using System;
using System.Collections.Generic;
using System.Linq;
using DOL.AI.Brain;
using DOL.GS.Effects;

namespace DOL.GS
{
    //Component for holding persistent Effects on the player//
    
    public class EffectListComponent
    {
        public long _lastChecked = 0;
        public GameLiving Owner;
        
        public int _lastUpdateEffectsCount = 0;
        
        public object _effectsLock = new object(); 
        public Dictionary<eEffect, List<ECSGameEffect>> Effects = new Dictionary<eEffect, List<ECSGameEffect>>();
        public Dictionary<int, ECSGameEffect> EffectIdToEffect = new Dictionary<int, ECSGameEffect>();

        /// <summary>
        /// Holds the concentration effects list
        /// </summary>
        private List<ECSGameSpellEffect> m_concEffects;

        public object _concentrationEffectsLock = new object();

        /// <summary>
        /// Gets the concentration effects list
        /// </summary>
        public List<ECSGameSpellEffect> ConcentrationEffects { get { return m_concEffects; } }

        public EffectListComponent(GameLiving p)
        {
            Owner = p;
            m_concEffects = new List<ECSGameSpellEffect>(20);
        }

        public bool AddEffect(ECSGameEffect effect)
        {
            lock (_effectsLock)
            {
                try
                {
                    // dead owners don't get effects
                    if (!Owner.IsAlive || Owner.ObjectState != GameObject.eObjectState.Active)
                        return false;

                    if (!EntityManager.GetLivingByComponent(typeof(EffectListComponent)).ToArray().Contains(Owner))
                    {
                        EntityManager.AddComponent(typeof(EffectListComponent), Owner);
                    }

                    // Check to prevent crash from holding sprint button down.
                    if (effect is ECSGameAbilityEffect)
                    {
                        if (!Effects.ContainsKey(effect.EffectType))
                            Effects.Add(effect.EffectType, new List<ECSGameEffect> { effect });
                        else if (effect.EffectType == eEffect.Protect || effect.EffectType == eEffect.Guard)
                        {
                            Effects[effect.EffectType].Add(effect);
                        }
                        return true;
                    }

                    ECSGameSpellEffect spellEffect = effect as ECSGameSpellEffect;
                    
                    //if (effect.EffectType == eEffect.OffensiveProc || effect.EffectType == eEffect.DefensiveProc)
                    //{
                    //    if (!Effects.ContainsKey(effect.EffectType))
                    //    {
                    //        Effects.Add(effect.EffectType, new List<ECSGameEffect> { effect });
                    //        EffectIdToEffect.Add(effect.Icon, effect);
                    //    }
                    //}
                    if (spellEffect != null && Effects.TryGetValue(effect.EffectType, out List<ECSGameEffect> existingGameEffects))
                    {
                        List<ECSGameSpellEffect> existingEffects = existingGameEffects.Cast<ECSGameSpellEffect>().ToList();

                        // Effects contains this effect already so refresh it
                        if (existingEffects.FirstOrDefault(e => e.SpellHandler.Spell.ID == spellEffect.SpellHandler.Spell.ID 
                                                                || (spellEffect.SpellHandler.Spell.EffectGroup > 0 && e.SpellHandler.Spell.EffectGroup == spellEffect.SpellHandler.Spell.EffectGroup && spellEffect.SpellHandler.Spell.IsPoisonEffect)) != null)
                        {
                            if (spellEffect.IsConcentrationEffect() && !spellEffect.RenewEffect) 
                                return false;
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                //Console.WriteLine($"Effect found: name {existingEffects[i].Name} poison? {existingEffects[i].SpellHandler.Spell.IsPoisonEffect} ID {existingEffects[i].SpellHandler.Spell.ID} EffectGroup {existingEffects[i].SpellHandler.Spell.EffectGroup}");
                                if ((existingEffects[i].SpellHandler.Spell.IsPulsing && spellEffect.SpellHandler.Caster.LastPulseCast == spellEffect.SpellHandler.Spell
                                    && existingEffects[i].SpellHandler.Spell.ID == spellEffect.SpellHandler.Spell.ID)
                                    || (existingEffects[i].SpellHandler.Spell.IsConcentration && spellEffect == existingEffects[i])
                                    || existingEffects[i].SpellHandler.Spell.ID == spellEffect.SpellHandler.Spell.ID)
                                   
                                {
                                    if (spellEffect.SpellHandler.Spell.IsPoisonEffect && spellEffect.EffectType == eEffect.DamageOverTime)
                                    {
                                        existingEffects[i].ExpireTick = spellEffect.ExpireTick;
                                        spellEffect.IsBuffActive = true;
                                    }
                                    else if (spellEffect.EffectType != eEffect.MovementSpeedDebuff)
                                    {
                                        spellEffect.IsDisabled = existingEffects[i].IsDisabled;
                                        spellEffect.IsBuffActive = existingEffects[i].IsBuffActive;

                                        if (spellEffect.SpellHandler.Spell.IsPulsing)
                                            spellEffect.RenewEffect = true;

                                        spellEffect.PreviousPosition = GetAllEffects().IndexOf(existingEffects[i]);
                                        Effects[spellEffect.EffectType][i] = spellEffect;
                                    }
                                    return true;
                                }
                            }
                        }
                        else if (effect.EffectType == eEffect.SavageBuff || effect.EffectType == eEffect.ArmorAbsorptionBuff)
                        {
                            if (effect.EffectType == eEffect.ArmorAbsorptionBuff)
                            {
                                for (var i = 0; i < existingEffects.Count; i++)
                                    // Better Effect so disable the current Effect
                                    if (spellEffect.SpellHandler.Spell.Value >
                                        existingEffects[i].SpellHandler.Spell.Value)
                                    {
                                        EffectService.RequestDisableEffect(existingEffects[i]);
                                        Effects[spellEffect.EffectType].Add(spellEffect);
                                        EffectIdToEffect.TryAdd(spellEffect.Icon, spellEffect);
                                        return true;
                                    }
                                    else
                                    {
                                        return false;
                                    }
                            }
                            // Player doesn't have this buff yet
                            else if (existingEffects.Where(e => e.SpellHandler.Spell.SpellType == spellEffect.SpellHandler.Spell.SpellType).Count() == 0)
                            {
                                Effects[spellEffect.EffectType].Add(spellEffect);
                                EffectIdToEffect.TryAdd(spellEffect.Icon, spellEffect);
                                return true;
                            }
                            else
                                return false;
                        }
                        else
                        {                           
                            bool addEffect = false;
                            // Check to see if we can add new Effect
                            for (int i = 0; i < existingEffects.Count; i++)
                            {
                                if (existingEffects[i].SpellHandler.IsOverwritable(spellEffect) || spellEffect.EffectType == eEffect.MovementSpeedDebuff)
                                {
                                    if (effect.EffectType != eEffect.Bladeturn)
                                    {
                                        if (spellEffect.SpellHandler.Spell.IsPoisonEffect)
                                        {
                                            addEffect = true;
                                        }
                                        // Better Effect so disable the current Effect
                                        else if (spellEffect.SpellHandler.Spell.Value > existingEffects[i].SpellHandler.Spell.Value ||
                                            spellEffect.SpellHandler.Spell.Damage > existingEffects[i].SpellHandler.Spell.Damage)
                                        {
                                            if (spellEffect.SpellHandler.Spell.IsHelpful && (spellEffect.SpellHandler.Caster != existingEffects[i].SpellHandler.Caster ||
                                                spellEffect.SpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects ||
                                                existingEffects[i].SpellHandler.SpellLine.KeyName == GlobalSpellsLines.Potions_Effects))
                                                EffectService.RequestDisableEffect(existingEffects[i]);
                                            else
                                                EffectService.RequestCancelEffect(existingEffects[i]);

                                            addEffect = true;
                                        }
                                        else if (spellEffect.SpellHandler.Spell.Value < existingEffects[i].SpellHandler.Spell.Value ||
                                            spellEffect.SpellHandler.Spell.Damage < existingEffects[i].SpellHandler.Spell.Damage)
                                        {
                                            if ((existingEffects[i].SpellHandler.Spell.IsConcentration && spellEffect.SpellHandler.Caster != existingEffects[i].SpellHandler.Caster)
                                                || existingEffects[i].SpellHandler.Spell.IsPulsing)
                                            {
                                                EffectService.RequestDisableEffect(spellEffect);
                                                addEffect = true;
                                            }
                                            else
                                                addEffect = false;
                                        }
                                    }
                                    else
                                    {
                                        // PBT should only replace itself
                                        if (!spellEffect.SpellHandler.Spell.IsPulsing)
                                        {
                                            // Self cast Bladeturns should never be overwritten
                                            if (existingEffects[i].SpellHandler.Spell.Target.ToLower() != "self")
                                            {
                                                EffectService.RequestCancelEffect(existingEffects[i]);
                                                addEffect = true;
                                            }
                                        }
                                    }
                                }
                                else if (spellEffect.SpellHandler.Spell.EffectGroup != existingEffects[i].SpellHandler.Spell.EffectGroup)
                                {
                                    addEffect = true;
                                }
                            }
                            if (addEffect)
                            {
                                Effects[spellEffect.EffectType].Add(spellEffect);
                                EffectIdToEffect.TryAdd(spellEffect.Icon, spellEffect);
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
                catch (Exception e)
                {
                    //Console.WriteLine($"Error adding an Effect {e}");
                    return false;
                }               
            }
            
        }

        public List<ECSGameEffect> GetAllEffects()
        {
            lock (_effectsLock)
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
            lock (_effectsLock)
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
            lock (_effectsLock)
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
            lock (_effectsLock)
            {
                return GetSpellEffects(effectType)?.OrderByDescending(e => e.IsDisabled).ThenByDescending(e => e.SpellHandler.Spell.Value).FirstOrDefault();
            }
        }

        public List<ECSGameSpellEffect> GetSpellEffects(eEffect effectType = eEffect.Unknown)
        {
            lock (_effectsLock)
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
            lock (_effectsLock)
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

        public bool RemoveEffect(ECSGameEffect effect)
        {
            lock (_effectsLock)
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
                            Effects[effect.EffectType].Remove(effect);
                            EffectIdToEffect.Remove(effect.Icon);

                            if (Effects[effect.EffectType].Count == 0)
                            {
                                EffectIdToEffect.Remove(effect.Icon);
                                Effects.Remove(effect.EffectType);
                            }

                            if (Effects.Count == 0)
                            {
                                EntityManager.RemoveComponent(typeof(EffectListComponent), Owner);
                            }
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
                {
                    return true;
                } 
                else
                {
                    return false;
                }
            } 
            catch (Exception e)
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