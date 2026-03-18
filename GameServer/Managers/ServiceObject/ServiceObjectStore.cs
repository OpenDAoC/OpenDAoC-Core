using System.Collections.Frozen;
using System.Collections.Generic;
using DOL.AI;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        /* 3 types of array are currently available:
         * 
         * ServiceObjectArray:
         *   Simple array with deferred additions and removals.
         *   Single threaded update.
         *   Doesn't support scheduling.
         *   Scales somewhat poorly with object count (O(n)).
         *   Memory efficient.
         *   Best for objects meant to be processed only once, or on every game loop tick.
         * 
         * SchedulableServiceObjectArray:
         *   Inherits ServiceObjectArray, controlled by a timing wheel.
         *   Single threaded update.
         *   Supports scheduling.
         *   Memory efficiency depends on its bucket count.
         *   Scales well with object count, but has slow updates if objects need to be rescheduled frequently.
         *   No use case for now.
         * 
         * ShardedServiceObjectArray:
         *   Backed by an array of SchedulableServiceObjectArray.
         *   Multithreaded update.
         *   Supports scheduling.
         *   Scales well with object count and has fairly fast updates (depending on CPU count).
         *   Memory efficiency depends on SchedulableServiceObjectArray's bucket count and CPU count.
         *   Best for everything that ticks much slower than the game loop, if object count is high enough to justify the overhead.
         */

        private static FrozenDictionary<ServiceObjectType, IServiceObjectArray> _serviceObjectArrays =
            new Dictionary<ServiceObjectType, IServiceObjectArray>()
            {
                { ServiceObjectType.Client, new ServiceObjectArray<GameClient>(Properties.MAX_PLAYERS) },
                { ServiceObjectType.Brain, new ShardedServiceObjectArray<ABrain>(Properties.MAX_ENTITIES) },
                { ServiceObjectType.AttackComponent, new ServiceObjectArray<AttackComponent>(1250) },
                { ServiceObjectType.CastingComponent, new ServiceObjectArray<CastingComponent>(1250) },
                { ServiceObjectType.Effect, new ShardedServiceObjectArray<ECSGameEffect>(10000) },
                { ServiceObjectType.EffectListComponent, new ServiceObjectArray<EffectListComponent>(1250) },
                { ServiceObjectType.MovementComponent, new ServiceObjectArray<MovementComponent>(1250) },
                { ServiceObjectType.CraftComponent, new ServiceObjectArray<CraftComponent>(100) },
                { ServiceObjectType.SubZoneObject, new ServiceObjectArray<SubZoneObject>(Properties.MAX_ENTITIES) },
                { ServiceObjectType.LivingBeingKilled, new ServiceObjectArray<LivingBeingKilled>(200) },
                { ServiceObjectType.Timer, new ShardedServiceObjectArray<ECSGameTimer>(500) }
            }.ToFrozenDictionary();

        public static bool Add<T>(T serviceObject) where T : class, IServiceObject
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;

            // Prevent re-entry if the object is already added, but not being removed or scheduled.
            if (id.IsActive && id.PeekAction() is not ServiceObjectId.PendingAction.Remove and not ServiceObjectId.PendingAction.Schedule)
                return false;

            // Prevent re-entry if the object is already being added.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Add))
                return false;

            (_serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArrayBase<T>).Add(serviceObject);
            return true;
        }

        // Schedule an object to be returned at a later time.
        // Scheduling in the past is allowed (the item will be returned immediately), and it's the caller's responsibility to ensure this makes sense.
        public static bool Schedule<T>(T serviceObject, long nextTickMs) where T : class, ISchedulableServiceObject
        {
            SchedulableServiceObjectId id = serviceObject.ServiceObjectId;

            // Prevent re-entry if the object is already being scheduled.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Schedule))
                return false;

            (_serviceObjectArrays[id.Type] as ServiceObjectArrayBase<T>).Schedule(serviceObject, nextTickMs);
            return true;
        }

        public static bool Remove<T>(T serviceObject) where T : class, IServiceObject
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;

            // Prevent re-entry if the object is already removed, but not being added or scheduled.
            if (!id.IsActive && id.PeekAction() is not ServiceObjectId.PendingAction.Add and not ServiceObjectId.PendingAction.Schedule)
                return false;

            // Prevent re-entry if the object is already being removed.
            if (!id.TrySetAction(ServiceObjectId.PendingAction.Remove))
                return false;

            (_serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArrayBase<T>).Remove(serviceObject);
            return true;
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        public static ServiceObjectView<T> UpdateAndGetView<T>(ServiceObjectType type) where T : class, IServiceObject
        {
            ServiceObjectArrayBase<T> array = _serviceObjectArrays[type] as ServiceObjectArrayBase<T>;
            array.Update(GameLoop.GameLoopTime);

            if (array.IsSharded)
                return new(array.Shards, array.ShardStartIndices, array.TotalValidCount);
            else
                return new(array.Items, array.LastValidIndex + 1);
        }
    }
}
