using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class EffectListService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<EffectListComponent> _effectListComponents;
        private int _entityCount;
        private int _lastValidIndex;

        public static EffectListService Instance { get; private set; }

        static EffectListService()
        {
            Instance = new();
        }

        public override void BeginTick()
        {
            ProcessPostedActions();
            try
            {
                _effectListComponents = ServiceObjectStore.UpdateAndGetAll<EffectListComponent>(ServiceObjectType.EffectListComponent, out _lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                _lastValidIndex = -1;
                return;
            }

            GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
        }

        public override void EndTick()
        {
            GameLoop.ExecuteWork(_lastValidIndex + 1, EndTickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref _entityCount, _effectListComponents.Count);
        }

        private void BeginTickInternal(int index)
        {
            EffectListComponent effectListComponent = null;

            try
            {
                effectListComponent = _effectListComponents[index];

                long startTick = GameLoop.GetRealTime();
                effectListComponent.BeginTick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {ServiceName}.{nameof(BeginTickInternal)} for: {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, effectListComponent, effectListComponent.Owner);
            }
        }

        private void EndTickInternal(int index)
        {
            EffectListComponent effectListComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                effectListComponent = _effectListComponents[index];

                long startTick = GameLoop.GetRealTime();
                effectListComponent.EndTick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {ServiceName}.{nameof(EndTickInternal)} for: {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, effectListComponent, effectListComponent.Owner);
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
