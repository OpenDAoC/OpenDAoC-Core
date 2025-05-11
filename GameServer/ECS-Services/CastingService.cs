using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using ECS.Debug;

namespace DOL.GS
{
    public static class CastingService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(CastingService);
        private static List<CastingComponent> _list;
        private static int _entityCount;

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);
            _list = EntityManager.UpdateAndGetAll<CastingComponent>(EntityManager.EntityType.CastingComponent, out int lastValidIndex);
            GameLoop.Work(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _list.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            CastingComponent castingComponent = _list[index];

            try
            {
                if (castingComponent?.EntityManagerId.IsSet != true)
                    return;

                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                long startTick = GameLoop.GetCurrentTime();
                castingComponent.Tick();
                long stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for: {castingComponent.Owner.Name}({castingComponent.Owner.ObjectID}) Spell: {castingComponent.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, castingComponent, castingComponent.Owner);
            }
        }
    }
}
