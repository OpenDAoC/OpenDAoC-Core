using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class CraftingService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(CraftingService);
        private static List<CraftComponent> _list;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<CraftComponent>(EntityManager.EntityType.CraftComponent, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            CraftComponent craftComponent = _list[index];

            try
            {
                if (craftComponent?.EntityManagerId.IsSet != true)
                    return;

                craftComponent.Tick();
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, craftComponent, craftComponent.Owner);
            }
        }
    }
}
