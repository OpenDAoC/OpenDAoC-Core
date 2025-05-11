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
            _list = EntityManager.UpdateAndGetAll<ABrain>(EntityManager.EntityType.Brain, out int lastValidIndex);
            GameLoop.Work(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            ABrain brain = _list[index];

            if (brain?.EntityManagerId.IsSet != true)
                return;

            if (Diagnostics.CheckEntityCounts)
                Interlocked.Increment(ref _entityCount);

            try
            {
                GameNPC npc = brain.Body;

                if (ServiceUtils.ShouldTickAdjust(ref brain.NextThinkTick))
                {
                    if (!brain.IsActive)
                    {
                        brain.Stop();
                        return;
                    }

                    long startTick = GameLoop.GetCurrentTime();
                    brain.Think();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > Diagnostics.LongTickThreshold)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {stopTick - startTick}ms");

                    brain.NextThinkTick += brain.ThinkInterval;
                }

                npc.effectListComponent.Tick();
                npc.movementComponent.Tick();
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, brain, brain.Body);
            }
        }
    }
}
