using System;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class ReaperService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceObjectView<LivingBeingKilled> _view;

        public static ReaperService Instance { get; }

        static ReaperService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _view = ServiceObjectStore.UpdateAndGetView<LivingBeingKilled>(ServiceObjectType.LivingBeingKilled);
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

        private static void TickInternal(LivingBeingKilled livingBeingKilled)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                TickMonitor monitor = new();
                livingBeingKilled.Killed.ProcessDeath(livingBeingKilled.Killer);

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {livingBeingKilled} Time: {elapsedMs}ms");
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
        public ServiceObjectId ServiceObjectId { get; } = new(ServiceObjectType.LivingBeingKilled);

        private LivingBeingKilled(GameLiving killed, GameObject killer)
        {
            Initialize(killed, killer);
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
