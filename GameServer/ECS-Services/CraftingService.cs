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

            List<CraftComponent> list = EntityManager.UpdateAndGetAll<CraftComponent>(EntityManager.EntityType.CraftComponent, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                CraftComponent craftComponent = list[i];

                if (craftComponent?.EntityManagerId.IsSet != true)
                    return;

                craftComponent.Tick(tick);
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
