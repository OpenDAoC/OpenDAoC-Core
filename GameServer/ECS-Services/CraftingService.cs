using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class CraftingService
    {
        private const string ServiceName = "CraftingService";

        static CraftingService()
        {
            EntityManager.AddService(typeof(CraftingService));
        }

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(ServiceName);

            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(CraftComponent));
            Parallel.ForEach(arr, p =>
            {
                if (p == null || p.craftComponent == null)
                {
                    return;
                }
                p.craftComponent.Tick(tick);
            });

            Diagnostics.StopPerfCounter(ServiceName);
        }

    }
}
