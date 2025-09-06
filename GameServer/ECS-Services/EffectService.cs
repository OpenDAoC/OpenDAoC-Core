using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.GS.PacketHandler;
using DOL.GS.Spells;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class EffectService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<ECSGameEffect> _list;
        private int _lastValidIndex;

        public static EffectService Instance { get; }

        static EffectService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ECSGameEffect>(ServiceObjectType.Effect, out _lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                _lastValidIndex = -1;
                return;
            }

            GameLoop.ExecuteForEach(_list, _lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _list.Count);
        }

        private static void TickInternal(ECSGameEffect effect)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                long startTick = GameLoop.GetRealTime();
                TickEffect(effect);
                long stopTick = GameLoop.GetRealTime();
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, effect, effect.Owner);
            }
        }

        private static void TickEffect(ECSGameEffect effect)
        {
            if (effect is ECSGameAbilityEffect abilityEffect)
                TickAbilityEffect(abilityEffect);
            else if (effect is ECSGameSpellEffect spellEffect)
                TickSpellEffect(spellEffect);
        }

        static void TickAbilityEffect(ECSGameAbilityEffect abilityEffect)
        {
            if (abilityEffect.NextTick != 0 && GameServiceUtils.ShouldTick(abilityEffect.NextTick))
            {
                abilityEffect.OnEffectPulse();
                abilityEffect.NextTick += abilityEffect.PulseFreq;
            }

            if (abilityEffect.Duration > 0 && GameServiceUtils.ShouldTick(abilityEffect.ExpireTick))
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

            if (GameServiceUtils.ShouldTick(spellEffect.ExpireTick))
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
        }

        static void TickConcentrationEffect(ECSGameSpellEffect spellEffect)
        {
            if (!GameServiceUtils.ShouldTick(spellEffect.NextTick))
                return;

            ISpellHandler spellHandler = spellEffect.SpellHandler;
            GameLiving caster = spellHandler.Caster;

            // We normally keep effects active on paused NPCs and let them expire naturally.
            // Concentration effects however should probably be stopped since they keep the effect list component permanently active for no good reason.
            // Checking `IsVisibleToPlayers` should be enough for this purpose.
            if (caster is GameNPC npcCaster && !npcCaster.IsVisibleToPlayers)
            {
                spellEffect.Stop();
                return;
            }

            spellEffect.Owner.effectListComponent.HandleConcentrationEffectRangeCheck(spellEffect);
            spellEffect.NextTick += spellEffect.PulseFreq;
        }

        static void TickPulsingEffect(ECSGameSpellEffect spellEffect, Spell spell, ISpellHandler spellHandler, GameLiving caster)
        {
            if (!GameServiceUtils.ShouldTick(spellEffect.NextTick))
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

                        if (GameServiceUtils.ShouldTick(childEffect.ExpireTick))
                        {
                            livings ??= GameLoop.GetListForTick<GameLiving>();
                            livings.Add(pair.Key);
                            childEffect.Stop();
                        }
                    }

                    if (livings != null)
                    {
                        foreach (GameLiving living in livings)
                            pulseEffect.ChildEffects.Remove(living);
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
