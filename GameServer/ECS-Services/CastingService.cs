using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class CastingService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(CastingService);
        private static List<CastingComponent> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<CastingComponent>(ServiceObjectType.CastingComponent, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                Diagnostics.StopPerfCounter(SERVICE_NAME);
                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            CastingComponent castingComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                castingComponent = _list[index];
                long startTick = GameLoop.GetRealTime();
                castingComponent.Tick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {castingComponent.Owner.Name}({castingComponent.Owner.ObjectID}) Spell: {castingComponent.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, castingComponent, castingComponent.Owner);
            }
        }
    }
}
