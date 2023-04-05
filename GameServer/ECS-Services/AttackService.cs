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
        private const string SERVICE_NAME = "AttackService";

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<AttackComponent> list = EntityManager.GetAll<AttackComponent>(EntityManager.EntityType.AttackComponent);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.AttackComponent) + 1, i =>
            {
                try
                {
                    AttackComponent a = list[i];

                    if (a == null)
                        return;

                    long startTick = GameTimer.GetTickCount();
                    a.Tick(tick);
                    long stopTick = GameTimer.GetTickCount();

                    if ((stopTick - startTick) > 25)
                        log.Warn($"Long AttackComponent.Tick for {a.owner.Name}({a.owner.ObjectID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in AttackService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
