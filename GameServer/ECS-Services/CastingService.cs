using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class CastingService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = "CastingService";

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<CastingComponent> list = EntityManager.GetAll<CastingComponent>(EntityManager.EntityType.CastingComponent);

            Parallel.For(0, EntityManager.GetLastNonNullIndex(EntityManager.EntityType.CastingComponent) + 1, i =>
            {
                try
                {
                    CastingComponent c = list[i];

                    if (c == null)
                        return;

                    long startTick = GameTimer.GetTickCount();
                    c.Tick(tick);
                    long stopTick = GameTimer.GetTickCount();

                    if ((stopTick - startTick) > 25)
                        log.Warn($"Long CastingComponent.Tick for: {c.Owner.Name}({c.Owner.ObjectID}) Spell: {c.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered in CastingService: {e}");
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }
    }
}
