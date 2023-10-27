using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Core.GS.Enums;
using Core.GS.GameLoop;
using log4net;

namespace Core.GS.ECS;

public static class CastingService
{
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
    private const string SERVICE_NAME = nameof(CastingService);

    public static void Tick(long tick)
    {
        GameLoopMgr.CurrentServiceTick = SERVICE_NAME;
        Diagnostics.StartPerfCounter(SERVICE_NAME);

        List<CastingComponent> list = EntityMgr.UpdateAndGetAll<CastingComponent>(EEntityType.CastingComponent, out int lastValidIndex);

        Parallel.For(0, lastValidIndex + 1, i =>
        {
            CastingComponent castingComponent = list[i];

            try
            {
                if (castingComponent?.EntityManagerId.IsSet != true)
                    return;

                long startTick = GameLoopMgr.GetCurrentTime();
                castingComponent.Tick(tick);
                long stopTick = GameLoopMgr.GetCurrentTime();

                if (stopTick - startTick > 25)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {castingComponent.Owner.Name}({castingComponent.Owner.ObjectID}) Spell: {castingComponent.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtil.HandleServiceException(e, SERVICE_NAME, castingComponent, castingComponent.Owner);
            }
        });

        Diagnostics.StopPerfCounter(SERVICE_NAME);
    }
}