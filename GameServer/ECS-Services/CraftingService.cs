using System;
using System.Collections.Generic;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class CraftingService
    {
        private const string SERVICE_NAME = nameof(CraftingService);
        private static List<CraftComponent> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = ServiceObjectStore.UpdateAndGetAll<CraftComponent>(ServiceObjectType.CraftComponent, out int lastValidIndex);
            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            CraftComponent craftComponent = _list[index];

            try
            {
                if (craftComponent?.ServiceObjectId.IsSet != true)
                    return;

                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                craftComponent.Tick();
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, craftComponent, craftComponent.Owner);
            }
        }
    }
}
