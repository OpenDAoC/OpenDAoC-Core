using System;
using System.Threading.Tasks;
using System.Reflection;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class AttackService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string ServiceName = "AttackService";

        static AttackService()
        {
            EntityManager.AddService(typeof(AttackService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);
            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(AttackComponent));

            Parallel.ForEach(arr, p =>
            {
                try
                {
                    if (p?.attackComponent == null)
                        return;

                    long startTick = GameTimer.GetTickCount();
                    p.attackComponent.Tick(tick);
                    long stopTick = GameTimer.GetTickCount();

                    if ((stopTick - startTick) > 25)
                        log.Warn($"Long AttackComponent.Tick for {p.Name}({p.ObjectID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in AttackService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }
    }
}
