using System.Collections.Generic;
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

            List<CraftComponent> list = EntityManager.GetAll<CraftComponent>(EntityManager.EntityType.CraftComponent);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.CraftComponent) + 1, i =>
            {
                list[i]?.Tick(tick);
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
