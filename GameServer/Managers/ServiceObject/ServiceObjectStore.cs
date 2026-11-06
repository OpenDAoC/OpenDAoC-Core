using System;
using DOL.AI;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        static ServiceObjectStore()
        {
            Register<GameClient>();
            Register<ABrain>();
            Register<AttackComponent>();
            Register<CastingComponent>();
            Register<ECSGameEffect>();
            Register<EffectListComponent>();
            Register<MovementComponent>();
            Register<CraftComponent>();
            Register<SubZoneObject>();
            Register<LivingBeingKilled>();
            Register<ECSGameTimer>();
        }

        public static bool Add(IServiceObject obj)
        {
            return Add(obj.ServiceObjectId);
        }

        public static bool Remove(IServiceObject obj)
        {
            return Remove(obj.ServiceObjectId);
        }

        public static bool Add(ServiceObjectId id)
        {
            return id.Register();
        }

        public static bool Remove(ServiceObjectId id)
        {
            return id.Unregister();
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        public static ServiceObjectView<T> UpdateAndGetView<T>() where T : class, IServiceObject<T>
        {
            ServiceObjectArrayBase<T> array = ViewSlot<T>.Array ??
                throw new InvalidOperationException($"{typeof(T)} is not registered in {nameof(ServiceObjectStore)}.");

            return array.UpdateAndGetView(GameLoop.GameLoopTime);
        }

        public static ServiceObjectArrayBase<T, TId> GetArray<T, TId>()
            where T : class, IServiceObject<T>
            where TId : ServiceObjectId<T>
        {
            return ArraySlot<T, TId>.Array ??
                throw new InvalidOperationException($"{typeof(T)} is not registered as a {typeof(TId).Name} owner. Check the interface it implements.");
        }

        public static void Publish<T, TId>(ServiceObjectArrayBase<T, TId> array)
            where T : class, IServiceObject<T>
            where TId : ServiceObjectId<T>
        {
            ViewSlot<T>.Array = array;
            ArraySlot<T, TId>.Array = array;
        }

        private static void Register<T>() where T : class, IServiceObject<T>
        {
            T.CreateArray(capacity: 100).Publish();
        }

        private static class ViewSlot<T> where T : class, IServiceObject<T>
        {
            public static ServiceObjectArrayBase<T> Array;
        }

        private static class ArraySlot<T, TId>
            where T : class, IServiceObject<T>
            where TId : ServiceObjectId<T>
        {
            public static ServiceObjectArrayBase<T, TId> Array;
        }
    }
}
