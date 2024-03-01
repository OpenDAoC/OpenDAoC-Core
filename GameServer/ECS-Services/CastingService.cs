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
        private const string SERVICE_NAME = nameof(CastingService);
        private static List<CastingComponent> _list;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<CastingComponent>(EntityManager.EntityType.CastingComponent, out int lastValidIndex);
            Parallel.For(0, lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            CastingComponent castingComponent = _list[index];

            try
            {
                if (castingComponent?.EntityManagerId.IsSet != true)
                    return;

                long startTick = GameLoop.GetCurrentTime();
                castingComponent.Tick();
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > 25)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {castingComponent.Owner.Name}({castingComponent.Owner.ObjectID}) Spell: {castingComponent.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, castingComponent, castingComponent.Owner);
            }
        }
    }
}
