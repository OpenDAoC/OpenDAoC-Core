using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;
using System.Reflection;

namespace DOL.GS
{
    public static class AttackService
    {
        static int _segmentsize = 1000;
        static List<Task> _tasks = new List<Task>();

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
                    if (p == null || p.attackComponent == null)
                    {
                        return;
                    }

                    long startTick = GameTimer.GetTickCount();
                    p.attackComponent.Tick(tick);
                    long stopTick = GameTimer.GetTickCount();
                    if((stopTick - startTick)  > 25 )
                        log.Warn($"Long AttackComponent.Tick for {p.Name}({p.ObjectID}) Time: {stopTick - startTick}ms");
                } catch (Exception e)
                {
                    log.Error($"Critical error encountered in Attack Service: {e}");
                }
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
