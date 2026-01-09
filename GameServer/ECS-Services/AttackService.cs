using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using DOL.Timing;
using ECS.Debug;

namespace DOL.GS
{
    public class AttackService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<AttackComponent> _list;

        public static AttackService Instance { get; }

        static AttackService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<AttackComponent>(ServiceObjectType.AttackComponent, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                return;
            }

            GameLoop.ExecuteForEach(_list, lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _list.Count);
        }

        private static void TickInternal(AttackComponent attackComponent)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                long startTick = MonotonicTime.NowMs;
                attackComponent.Tick();
                long stopTick = MonotonicTime.NowMs;

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, attackComponent, attackComponent.owner);
            }
        }
    }
}
