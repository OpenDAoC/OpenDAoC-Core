using System;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public class AttackService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceObjectView<AttackComponent> _view;

        public static AttackService Instance { get; }

        static AttackService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _view = ServiceObjectStore.UpdateAndGetView<AttackComponent>(ServiceObjectType.AttackComponent);
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

        private static void TickInternal(AttackComponent attackComponent)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                TickMonitor monitor = new();
                attackComponent.Tick();

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {elapsedMs}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, attackComponent, attackComponent.owner);
            }
        }
    }
}
