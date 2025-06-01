using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public class ReaperService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ReaperService);
        private static List<LivingBeingKilled> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = ServiceObjectStore.UpdateAndGetAll<LivingBeingKilled>(ServiceObjectType.LivingBeingKilled, out int lastValidIndex);
            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            LivingBeingKilled livingBeingKilled = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                livingBeingKilled = _list[index];
                long startTick = GameLoop.GetCurrentTime();
                livingBeingKilled.Killed.ProcessDeath(livingBeingKilled.Killer);
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {livingBeingKilled} Time: {stopTick - startTick}ms");

                ServiceObjectStore.Remove(livingBeingKilled);
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, livingBeingKilled, livingBeingKilled.Killed);
            }
            finally
            {
                if (livingBeingKilled != null)
                    ServiceObjectStore.Remove(livingBeingKilled);
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
