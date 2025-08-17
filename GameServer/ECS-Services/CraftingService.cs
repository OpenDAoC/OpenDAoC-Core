using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class CraftingService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<CraftComponent> _list;
        private int _entityCount;

        public static CraftingService Instance { get; private set; }

        static CraftingService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<CraftComponent>(ServiceObjectType.CraftComponent, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref _entityCount, _list.Count);
        }

        private void TickInternal(int index)
        {
            CraftComponent craftComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                craftComponent = _list[index];
                craftComponent.Tick();
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, craftComponent, craftComponent.Owner);
            }
        }
    }
}
