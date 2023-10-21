using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using log4net;

namespace Core.GS.ECS;

public static class CraftingService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string SERVICE_NAME = nameof(CraftingService);

    public static void Tick(long tick)
    {
        GameLoop.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        List<CraftComponent> list = EntityMgr.UpdateAndGetAll<CraftComponent>(EEntityType.CraftComponent, out int lastValidIndex);

        Parallel.For(0, lastValidIndex + 1, i =>
        {
            CraftComponent craftComponent = list[i];

            try
            {
                if (craftComponent?.EntityManagerId.IsSet != true)
                    return;

                craftComponent.Tick(tick);
            }
            catch (Exception e)
            {
                ServiceUtil.HandleServiceException(e, SERVICE_NAME, craftComponent, craftComponent.Owner);
            }
        });

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}