using System;
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

        private ServiceObjectView<ABrain> _view;

        public static NpcService Instance { get; }

        static NpcService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _view = ServiceObjectStore.UpdateAndGetView<ABrain>(ServiceObjectType.Brain);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetView)} failed. Skipping this tick.", e);

                return;
            }

            _view.ExecuteForEach(TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _view.TotalValidCount);
        }

        private static void TickInternal(ABrain brain)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                if (!GameServiceUtils.ShouldTick(brain.NextThinkTick))
                    return;

                GameNPC npc = brain.Body;

                if (!brain.IsActive)
                {
                    brain.Stop();
                    return;
                }

                TickMonitor monitor = new();
                brain.Think();

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {npc.Name}({npc.ObjectID}) Interval: {brain.ThinkInterval} BrainType: {brain.GetType()} Time: {elapsedMs}ms");

                brain.NextThinkTick = GameLoop.GameLoopTime + brain.ThinkInterval;
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, brain, brain.Body);
            }
        }
    }
}
