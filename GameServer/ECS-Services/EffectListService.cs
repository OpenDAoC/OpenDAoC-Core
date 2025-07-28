using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class EffectListService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(EffectListService);
        private const string SERVICE_NAME_BEGIN = $"{SERVICE_NAME}_Begin";
        private const string SERVICE_NAME_END = $"{SERVICE_NAME}_End";
        private static List<EffectListComponent> _effectListComponents;
        private static int _entityCount;
        private static int _lastValidIndex;

        public static void BeginTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_BEGIN;
            Diagnostics.StartPerfCounter(SERVICE_NAME_BEGIN);

            try
            {
                _effectListComponents = ServiceObjectStore.UpdateAndGetAll<EffectListComponent>(ServiceObjectType.EffectListComponent, out _lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                _lastValidIndex = -1;
                Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
                return;
            }

            GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
        }

        public static void EndTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_END;
            Diagnostics.StartPerfCounter(SERVICE_NAME_END);

            GameLoop.ExecuteWork(_lastValidIndex + 1, EndTickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME_END, ref _entityCount, _effectListComponents.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME_END);
        }

        private static void BeginTickInternal(int index)
        {
            EffectListComponent effectListComponent = null;

            try
            {
                effectListComponent = _effectListComponents[index];

                long startTick = GameLoop.GetRealTime();
                effectListComponent.BeginTick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME_BEGIN}.{nameof(BeginTickInternal)} for: {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME_BEGIN, effectListComponent, effectListComponent.Owner);
            }
        }

        private static void EndTickInternal(int index)
        {
            EffectListComponent effectListComponent = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                effectListComponent = _effectListComponents[index];

                long startTick = GameLoop.GetRealTime();
                effectListComponent.EndTick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME_END}.{nameof(EndTickInternal)} for: {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME_END, effectListComponent, effectListComponent.Owner);
            }
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.None)
        {
            if (spellType is eSpellType.None)
            {
                List<ECSGameEffect> effects = target.effectListComponent.GetEffects(effectType);
                return effects.FirstOrDefault();
            }
            else
                return GetSpellEffectOnTarget(target, effectType, spellType);
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType)
        {
            List<ECSGameSpellEffect> effects = target.effectListComponent.GetSpellEffects(effectType);
            return effects.FirstOrDefault(e => e.SpellHandler.Spell.SpellType == spellType);
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            List<ECSGameAbilityEffect> effects = target.effectListComponent.GetAbilityEffects(effectType);
            return effects.FirstOrDefault();
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            List<ECSGameEffect> effects = target.effectListComponent.GetEffects(effectType);
            return effects.FirstOrDefault(e => e is ECSImmunityEffect) as ECSImmunityEffect;
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            List<ECSPulseEffect> effects = target.effectListComponent.GetPulseEffects();
            return effects?.FirstOrDefault(e => e.SpellHandler.Spell == spell);
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            if (!target.effectListComponent.ContainsEffectForEffectType(effectType))
                return false;

            ECSGameEffect effectToCancel = GetEffectOnTarget(target, effectType);

            if (effectToCancel == null)
                return false;

            return effectToCancel.Stop();
        }
    }
}
