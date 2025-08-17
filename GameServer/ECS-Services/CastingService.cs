using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class CastingService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private List<CastingComponent> _list;
        private int _entityCount;

        public static CastingService Instance { get; private set; }

        static CastingService()
        {
            Instance = new();
        }

        public override void Tick()
        {
            ProcessPostedActions();
            int lastValidIndex;

            try
            {
                _list = ServiceObjectStore.UpdateAndGetAll<CastingComponent>(ServiceObjectType.CastingComponent, out lastValidIndex);
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                return;
            }

            GameLoop.ExecuteWork(lastValidIndex + 1, TickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref _entityCount, _list.Count);
        }

        private void TickInternal(int index)
        {
            CastingComponent castingComponent = null;

            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref _entityCount);

                castingComponent = _list[index];
                long startTick = GameLoop.GetRealTime();
                castingComponent.Tick();
                long stopTick = GameLoop.GetRealTime();

                if (stopTick - startTick > Diagnostics.LongTickThreshold)
                    log.Warn($"Long {ServiceName}.{nameof(Tick)} for: {castingComponent.Owner.Name}({castingComponent.Owner.ObjectID}) Spell: {castingComponent.SpellHandler?.Spell?.Name} Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, ServiceName, castingComponent, castingComponent.Owner);
            }
        }
    }
}
