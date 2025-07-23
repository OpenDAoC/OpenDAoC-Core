using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI;
using ECS.Debug;

namespace DOL.GS
{
    public static class NpcService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(NpcService);
        private static List<ABrain> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ABrain>(ServiceObjectType.Brain, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                Diagnostics.StopPerfCounter(SERVICE_NAME);
                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ABrain brain = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                brain = _list[index];
                GameNPC npc = brain.Body;

                if (ServiceUtils.ShouldTick(brain.NextThinkTick))
                {
                    if (!brain.IsActive)
                    {
                        brain.Stop();
                        return;
                    }

                    long startTick = GameLoop.GetRealTime();
                    brain.Think();
                    long stopTick = GameLoop.GetRealTime();

                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                    brain.NextThinkTick = GameLoop.GameLoopTime + brain.ThinkInterval;
                }
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, brain, brain.Body);
            }
        }
    }
}
