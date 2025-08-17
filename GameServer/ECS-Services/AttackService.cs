using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public class AttackService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<AttackComponent> _list;
        private int _entityCount;

        public static AttackService Instance { get; private set; }

        static AttackService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
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

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref _entityCount, _list.Count);
        }

        private void TickInternal(int index)
        {
            AttackComponent attackComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                attackComponent = _list[index];
                long startTick = GameLoop.GetRealTime();
                attackComponent.Tick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {ServiceName}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, attackComponent, attackComponent.owner);
            }
        }
    }
}
