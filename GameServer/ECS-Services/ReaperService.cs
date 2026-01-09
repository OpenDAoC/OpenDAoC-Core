using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using DOL.Timing;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class ReaperService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<LivingBeingKilled> _list;

        public static ReaperService Instance { get; }

        static ReaperService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<LivingBeingKilled>(ServiceObjectType.LivingBeingKilled, out lastValidIndex);
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

        private static void TickInternal(LivingBeingKilled livingBeingKilled)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                long startTick = MonotonicTime.NowMs;
                livingBeingKilled.Killed.ProcessDeath(livingBeingKilled.Killer);
                long stopTick = MonotonicTime.NowMs;

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {livingBeingKilled} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, livingBeingKilled, livingBeingKilled.Killed);
            }
            finally
            {
                if (livingBeingKilled != null)
                {
                    ServiceObjectStore.Remove(livingBeingKilled);
                    livingBeingKilled.Killed.OnReaperServiceHandlingComplete();
                }
            }
        }

        public static void KillLiving(GameLiving killed, GameObject killer)
        {
            LivingBeingKilled.Create(killed, killer);
        }
    }

    // Temporary objects to be added to `ServiceObjectStore` and consumed by `ReaperService`, representing a living object being killed and waiting to be processed.
    public class LivingBeingKilled : IServiceObject
    {
        public GameLiving Killed { get; private set; }
        public GameObject Killer { get; private set; }
        public ServiceObjectId ServiceObjectId { get; set; }

        private LivingBeingKilled(GameLiving killed, GameObject killer)
        {
            Initialize(killed, killer);
            ServiceObjectId = new ServiceObjectId(ServiceObjectType.LivingBeingKilled);
        }

        public static void Create(GameLiving killed, GameObject killer)
        {
            LivingBeingKilled livingBeingKilled = new(killed, killer);
            ServiceObjectStore.Add(livingBeingKilled);
        }

        private void Initialize(GameLiving killed, GameObject killer)
        {
            Killed = killed;
            Killer = killer;
        }

        public override string ToString()
        {
            return $"(Killed: {Killed}) (Killer: {Killer})";
        }
    }
}
