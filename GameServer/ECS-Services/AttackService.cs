using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class AttackService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(AttackService);
        private static List<AttackComponent> _list;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<AttackComponent>(EntityManager.EntityType.AttackComponent, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            AttackComponent attackComponent = _list[index];

            try
            {
                if (attackComponent?.EntityManagerId.IsSet != true)
                    return;

                long startTick = GameLoop.GetCurrentTime();
                attackComponent.Tick();
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > 25)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {attackComponent.owner.Name}({attackComponent.owner.ObjectID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, attackComponent, attackComponent.owner);
            }
        }
    }
}
