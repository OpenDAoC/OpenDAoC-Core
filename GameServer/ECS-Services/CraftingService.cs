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

            CraftComponent[] arr = EntityManager.GetAll<CraftComponent>(EntityManager.EntityType.CraftComponent);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.CraftComponent) + 1, i =>
            {
                arr[i]?.Tick(tick);
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
