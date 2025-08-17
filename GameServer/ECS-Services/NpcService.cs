using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class NpcService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static List<ABrain> _list;
        private static int _entityCount;

        public static NpcService Instance { get; private set; }

        static NpcService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<ABrain>(ServiceObjectType.Brain, out lastValidIndex);
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
            ABrain brain = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                brain = _list[index];
                GameNPC npc = brain.Body;

                if (GameServiceUtils.ShouldTick(brain.NextThinkTick))
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
                        log.Warn($"Long {ServiceName}.{nameof(Tick)} for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                    brain.NextThinkTick = GameLoop.GameLoopTime + brain.ThinkInterval;
                }
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, brain, brain.Body);
            }
        }
    }
}
