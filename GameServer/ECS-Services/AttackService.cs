using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class AttackService
    {
        static int _segmentsize = 1000;
        static List<Task> _tasks = new List<Task>();

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
                if (p == null || p.attackComponent == null)
                {
                    return;
                }
                p.attackComponent.Tick(tick);
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
