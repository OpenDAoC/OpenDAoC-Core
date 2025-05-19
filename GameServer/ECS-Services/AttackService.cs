using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class AttackService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(AttackService);
        private static List<AttackComponent> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(ServiceObjectType.AttackComponent, out int lastValidIndex);
            GameLoop.Work(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            AttackComponent attackComponent = _list[index];

            try
            {
                if (attackComponent?.ServiceObjectId.IsSet != true)
                    return;

                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                long startTick = GameLoop.GetCurrentTime();
                attackComponent.Tick();
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, attackComponent, attackComponent.owner);
            }
        }
    }
}
