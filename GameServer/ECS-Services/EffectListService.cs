using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class EffectListService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceObjectView<EffectListComponent> _view;

        public static EffectListService Instance { get; }

        static EffectListService()
        {
            Instance = new();
        }

        public override void BeginTick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _view = ServiceObjectStore.UpdateAndGetView<EffectListComponent>(ServiceObjectType.EffectListComponent);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetView)} failed. Skipping this tick.", e);

                return;
            }

            _view.ExecuteForEach(BeginTickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _view.TotalValidCount);
        }

        private static void BeginTickInternal(EffectListComponent effectListComponent)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                TickMonitor monitor = new();
                effectListComponent.BeginTick();

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(BeginTickInternal)} for {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {elapsedMs}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, effectListComponent, effectListComponent.Owner);
            }
        }

        public override void EndTick()
        {
            _view.ExecuteForEach(EndTickInternal);
        }

        private static void EndTickInternal(EffectListComponent effectListComponent)
        {
            try
            {
                TickMonitor monitor = new();
                effectListComponent.EndTick();

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(EndTickInternal)} for {effectListComponent.Owner.Name}({effectListComponent.Owner.ObjectID}) Time: {elapsedMs}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, effectListComponent, effectListComponent.Owner);
            }
        }

        public static ECSGameEffect GetEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Unknown)
        {
            if (spellType is eSpellType.Unknown)
            {
                List<ECSGameEffect> effects = target.effectListComponent.GetEffects(effectType);
                return effects.Count > 0 ? effects[0] : null;
            }
            else
                return GetSpellEffectOnTarget(target, effectType, spellType);
        }

        public static ECSGameSpellEffect GetSpellEffectOnTarget(GameLiving target, eEffect effectType, eSpellType spellType = eSpellType.Unknown)
        {
            List<ECSGameSpellEffect> effects = target.effectListComponent.GetSpellEffects(effectType);

            foreach (ECSGameSpellEffect effect in effects)
            {
                if (spellType is eSpellType.Unknown || effect.SpellHandler.Spell.SpellType == spellType)
                    return effect;
            }

            return null;
        }

        public static ECSGameAbilityEffect GetAbilityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            List<ECSGameAbilityEffect> effects = target.effectListComponent.GetAbilityEffects(effectType);
            return effects.Count > 0 ? effects[0] : null;
        }

        public static ECSImmunityEffect GetImmunityEffectOnTarget(GameLiving target, eEffect effectType)
        {
            List<ECSGameEffect> effects = target.effectListComponent.GetEffects(effectType);

            foreach (ECSGameEffect effect in effects)
            {
                if (effect is ECSImmunityEffect immunity)
                    return immunity;
            }

            return null;
        }

        public static ECSPulseEffect GetPulseEffectOnTarget(GameLiving target, Spell spell)
        {
            List<ECSPulseEffect> effects = target.effectListComponent.GetPulseEffects();

            foreach (ECSPulseEffect effect in effects)
            {
                if (effect.SpellHandler.Spell == spell)
                    return effect;
            }

            return null;
        }

        public static bool TryCancelFirstEffectOfTypeOnTarget(GameLiving target, eEffect effectType)
        {
            if (!target.effectListComponent.ContainsEffectForEffectType(effectType))
                return false;

            ECSGameEffect effectToCancel = GetEffectOnTarget(target, effectType);

            if (effectToCancel == null)
                return false;

            return effectToCancel.End();
        }
    }
}
