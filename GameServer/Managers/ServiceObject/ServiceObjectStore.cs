using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<ServiceObjectType, IServiceObjectArray> _serviceObjectArrays = new()
        {
            { ServiceObjectType.Client, new ServiceObjectArray<GameClient>(ServerProperties.Properties.MAX_PLAYERS) },
            { ServiceObjectType.Brain, new ServiceObjectArray<ABrain>(ServerProperties.Properties.MAX_ENTITIES) },
            { ServiceObjectType.AttackComponent, new ServiceObjectArray<AttackComponent>(1250) },
            { ServiceObjectType.CastingComponent, new ServiceObjectArray<CastingComponent>(1250) },
            { ServiceObjectType.CraftComponent, new ServiceObjectArray<CraftComponent>(100) },
            { ServiceObjectType.ObjectChangingSubZone, new ServiceObjectArray<ObjectChangingSubZone>(ServerProperties.Properties.MAX_ENTITIES) },
            { ServiceObjectType.LivingBeingKilled, new ServiceObjectArray<LivingBeingKilled>(200) },
            { ServiceObjectType.Timer, new ServiceObjectArray<ECSGameTimer>(500) }
        };

        public static bool Add<T>(T serviceObject) where T : class, IServiceObject
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;

            // Return false if the service object is already present and not being removed.
            if (id.IsSet && !id.IsPendingRemoval)
                return false;

            (_serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArray<T>).Add(serviceObject);
            return true;
        }

        public static bool Remove<T>(T serviceObject) where T : class, IServiceObject
        {
            ServiceObjectId id = serviceObject.ServiceObjectId;

            // Return false if the service object is absent and not being added.
            if (!id.IsSet && !id.IsPendingAddition) 
                return false;

            (_serviceObjectArrays[serviceObject.ServiceObjectId.Type] as ServiceObjectArray<T>).Remove(serviceObject);
            return true;
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        // Thread unsafe. The returned list should not be modified.
        // Elements should be null checked alongside the value returned by `ServiceObjectId.IsSet`.
        public static List<T> UpdateAndGetAll<T>(ServiceObjectType type, out int lastValidIndex) where T : class, IServiceObject
        {
            ServiceObjectArray<T> array = _serviceObjectArrays[type] as ServiceObjectArray<T>;
            lastValidIndex = array.Update();
            return array.Items;
        }

        private class ServiceObjectArray<T> : IServiceObjectArray where T : class, IServiceObject
        {
            private SortedSet<int> _invalidIndexes = new();
            private DrainArray<T> _itemsToAdd  = new();
            private DrainArray<T> _itemsToRemove = new();
            private int _updating = new();
            private int _lastValidIndex = -1;

            public List<T> Items { get; }

            public ServiceObjectArray(int capacity)
            {
                Items = new List<T>(capacity);
            }

            public void Add(T item)
            {
                item.ServiceObjectId.OnPreAdd();
                _itemsToAdd.Add(item);
            }

            public void Remove(T item)
            {
                item.ServiceObjectId.OnPreRemove();
                _itemsToRemove.Add(item);
            }

            public int Update()
            {
                if (Interlocked.Exchange(ref _updating, 1) != 0)
                    throw new InvalidOperationException();

                try
                {
                    if (_itemsToRemove.Any)
                    {
                        _itemsToRemove.DrainTo(static (item, array) =>
                        {
                            ServiceObjectId id = item.ServiceObjectId;

                            if (id.IsPendingRemoval && id.IsSet)
                                array.RemoveInternal(id.Value);
                        }, this);
                    }

                    while (_lastValidIndex > -1)
                    {
                        if (Items[_lastValidIndex]?.ServiceObjectId.IsSet == true)
                            break;

                        _lastValidIndex--;
                    }

                    if (_itemsToAdd.Any)
                    {
                        _itemsToAdd.DrainTo(static (item, array) =>
                        {
                            ServiceObjectId id = item.ServiceObjectId;

                            if (id.IsPendingAddition && !id.IsSet)
                                id.Value = array.AddInternal(item);
                        }, this);
                    }
                }
                finally
                {
                    _updating = 0;
                }

                return _lastValidIndex;
            }

            private int AddInternal(T item)
            {
                if (_invalidIndexes.Count > 0)
                {
                    int index = _invalidIndexes.Min;
                    _invalidIndexes.Remove(index);
                    Items[index] = item;

                    if (index > _lastValidIndex)
                        _lastValidIndex = index;

                    return index;
                }

                // Increase the capacity of the list in the event that it's too small. This is a costly operation.
                // 'Add' already does it, but we nay want to know when it happens and control by how much it grows ('Add' would double it).
                if (++_lastValidIndex >= Items.Capacity)
                {
                    int newCapacity = (int) (Items.Capacity * 1.2);

                    if (log.IsWarnEnabled)
                        log.Warn($"{typeof(T)} {nameof(Items)} is too short. Resizing it to {newCapacity}.");

                    Items.Resize(newCapacity);
                }

                Items.Add(item);
                return _lastValidIndex;
            }

            private void RemoveInternal(int id)
            {
                T item = Items[id];

                if (id == Items.Count)
                    _lastValidIndex--;

                ServiceObjectId serviceObjectId = item.ServiceObjectId;
                serviceObjectId.Unset();
                _invalidIndexes.Add(id);
                Items[id] = null;
            }
        }

        private interface IServiceObjectArray { }
    }
}
