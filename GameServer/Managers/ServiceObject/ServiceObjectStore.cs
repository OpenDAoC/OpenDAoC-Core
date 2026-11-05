using System;
using System.Linq;
using DOL.AI;
using DOL.GS.ServerProperties;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        private static readonly IServiceObjectArray[] _serviceObjectArrays = CreateServiceObjectArrays();

        public static bool Add(IServiceObject serviceObject)
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;
            return _serviceObjectArrays[(int) id.Type].Add(serviceObject);
        }

        public static bool Remove(IServiceObject serviceObject)
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;
            return _serviceObjectArrays[(int) id.Type].Remove(serviceObject);
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        public static ServiceObjectView<T> UpdateAndGetView<T>(ServiceObjectType type) where T : class, IServiceObject
        {
            return ((ServiceObjectArrayBase<T>) _serviceObjectArrays[(int) type]).UpdateAndGetView(GameLoop.GameLoopTime);
        }

        private static IServiceObjectArray[] CreateServiceObjectArrays()
        {
            int size = Enum.GetValues<ServiceObjectType>().Max(t => (int) t) + 1;
            IServiceObjectArray[] arrays = new IServiceObjectArray[size];

            arrays[(int) ServiceObjectType.Client]              = CreateArray<GameClient>(Properties.MAX_PLAYERS);
            arrays[(int) ServiceObjectType.Brain]               = CreateArray<ABrain>(Properties.MAX_ENTITIES);
            arrays[(int) ServiceObjectType.AttackComponent]     = CreateArray<AttackComponent>(1250);
            arrays[(int) ServiceObjectType.CastingComponent]    = CreateArray<CastingComponent>(1250);
            arrays[(int) ServiceObjectType.Effect]              = CreateArray<ECSGameEffect>(10000);
            arrays[(int) ServiceObjectType.EffectListComponent] = CreateArray<EffectListComponent>(1250);
            arrays[(int) ServiceObjectType.MovementComponent]   = CreateArray<MovementComponent>(1250);
            arrays[(int) ServiceObjectType.CraftComponent]      = CreateArray<CraftComponent>(100);
            arrays[(int) ServiceObjectType.SubZoneObject]       = CreateArray<SubZoneObject>(Properties.MAX_ENTITIES);
            arrays[(int) ServiceObjectType.LivingBeingKilled]   = CreateArray<LivingBeingKilled>(200);
            arrays[(int) ServiceObjectType.Timer]               = CreateArray<ECSGameTimer>(500);

            return arrays;
        }

        private static IServiceObjectArray CreateArray<T>(int capacity) where T : class, IServiceObject
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
             *   Scales well with object count, and has fairly fast updates (depending on CPU count) but with a flat overhead.
             *   Memory efficiency depends on SchedulableServiceObjectArray's bucket count and CPU count.
             *   Best for everything that ticks much slower than the game loop, if object count is high enough to justify the overhead.
             */

            Type type = typeof(T);
            Type arrayType;

            if (typeof(IShardedServiceObject).IsAssignableFrom(type))
                arrayType = typeof(ShardedServiceObjectArray<>).MakeGenericType(type);
            else if (typeof(ISchedulableServiceObject).IsAssignableFrom(type))
                arrayType = typeof(SchedulableServiceObjectArray<>).MakeGenericType(type);
            else
                arrayType = typeof(ServiceObjectArray<>).MakeGenericType(type);

            return (IServiceObjectArray) Activator.CreateInstance(arrayType, capacity);
        }
    }
}
