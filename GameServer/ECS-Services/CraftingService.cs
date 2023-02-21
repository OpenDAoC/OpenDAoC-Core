using System.Threading.Tasks;
using ECS.Debug;

namespace DOL.GS
{
    public static class CraftingService
    {
        private const string SERVICE_NAME = "CraftingService";

        public static void Tick(long tick)
        {
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            GameLiving[] arr = EntityManager.GetLivingByComponent(typeof(CraftComponent));
            Parallel.ForEach(arr, p =>
            {
                if (p == null || p.craftComponent == null)
                {
                    return;
                }
                p.craftComponent.Tick(tick);
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
