using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class MovementService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(MovementService);
        private static List<MovementComponent> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = ServiceObjectStore.UpdateAndGetAll<MovementComponent>(ServiceObjectType.MovementComponent, out int lastValidIndex);
            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            MovementComponent movementComponent = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                movementComponent = _list[index];
                long startTick = GameLoop.GetRealTime();
                movementComponent.Tick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {movementComponent.Owner.Name}({movementComponent.Owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, movementComponent, movementComponent.Owner);
            }
        }
    }
}
