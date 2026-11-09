using System;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        private const int DEFAULT_CAPACITY = 100;

        // Discovers all service object types in this assembly and publishes their arrays.
        // Must run before any service objects are registered.
        public static bool Init()
        {
            foreach (Type type in typeof(ServiceObjectStore).Assembly.GetTypes())
            {
                foreach (Type interfaceType in type.GetInterfaces())
                {
                    if (!interfaceType.IsGenericType ||
                        interfaceType.GetGenericTypeDefinition() != typeof(IServiceObject<>) ||
                        interfaceType.GetGenericArguments()[0] != type)
                    {
                        continue;
                    }

                    IRegistrar registrar = (IRegistrar) Activator.CreateInstance(typeof(Registrar<>).MakeGenericType(type));
                    registrar.Register();
                    break;
                }
            }

            return true;
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        public static ServiceObjectView<T> UpdateAndGetView<T>(long now) where T : class, IServiceObject<T>
        {
            ServiceObjectArrayBase<T> array = ViewSlot<T>.Array ??
                throw new InvalidOperationException($"{typeof(T)} is not registered in {nameof(ServiceObjectStore)}.");

            return array.UpdateAndGetView(now);
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

        public static void Register<T>(int capacity = DEFAULT_CAPACITY) where T : class, IServiceObject<T>
        {
            T.CreateArray(capacity).Publish();
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

        private interface IRegistrar
        {
            void Register();
        }

        private sealed class Registrar<T> : IRegistrar where T : class, IServiceObject<T>
        {
            public void Register()
            {
                Register<T>();
            }
        }
    }
}
