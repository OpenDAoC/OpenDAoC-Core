using System;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class MovementService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private ServiceObjectView<MovementComponent> _view;

        public static MovementService Instance { get; }

        static MovementService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActionsParallel();

            try
            {
                _view = ServiceObjectStore.UpdateAndGetView<MovementComponent>(ServiceObjectType.MovementComponent);
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

        private static void TickInternal(MovementComponent movementComponent)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                TickMonitor monitor = new();
                movementComponent.Tick();

                if (monitor.IsLongTick(out long elapsedMs) && log.IsWarnEnabled)
                    log.Warn($"Long {Instance.ServiceName}.{nameof(Tick)} for {movementComponent.Owner.Name}({movementComponent.Owner.ObjectID}) Time: {elapsedMs}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, movementComponent, movementComponent.Owner);
            }
        }
    }
}
