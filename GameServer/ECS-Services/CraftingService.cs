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
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<CraftComponent> list = EntityManager.UpdateAndGetAll<CraftComponent>(EntityManager.EntityType.CraftComponent, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                list[i]?.Tick(tick);
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
