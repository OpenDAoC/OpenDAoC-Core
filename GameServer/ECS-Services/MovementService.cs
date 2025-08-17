using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class MovementService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<MovementComponent> _list;
        private int _entityCount;

        public static MovementService Instance { get; private set; }

        static MovementService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<MovementComponent>(ServiceObjectType.MovementComponent, out lastValidIndex);
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
            MovementComponent movementComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                movementComponent = _list[index];
                long startTick = GameLoop.GetRealTime();
                movementComponent.Tick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {ServiceName}.{nameof(Tick)} for: {movementComponent.Owner.Name}({movementComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, movementComponent, movementComponent.Owner);
            }
        }
    }
}
