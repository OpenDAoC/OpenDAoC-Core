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

        public static bool TryReuse<T>(ServiceObjectType type, out T serviceObject, out int index) where T : class, IServiceObject
        {
            // The returned index must be set by the caller, so that the service object can be initialized before being handled by the services.
            return (_serviceObjectArrays[type] as ServiceObjectArray<T>).TryReuse(out serviceObject, out index);
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
            private Stack<T> _itemsToAdd  = new();
            private Stack<T> _itemsToRemove = new();
            private readonly Lock _updateLock = new();
            private readonly Lock _itemsToAddLock = new();
            private readonly Lock _itemsToRemoveLock = new();
            private int _lastValidIndex = -1;

            public List<T> Items { get; }

            public ServiceObjectArray(int capacity)
            {
                Items = new List<T>(capacity);
            }

            public void Add(T item)
            {
                item.ServiceObjectId.OnPreAdd();

                lock (_itemsToAddLock)
                {
                    _itemsToAdd.Push(item);
                }
            }

            public bool TryReuse(out T item, out int index)
            {
                index = ServiceObjectId.UNSET_ID;
                item = null;

                lock (_updateLock)
                {
                    if (_invalidIndexes.Count == 0)
                        return false;

                    index = _invalidIndexes.Min;
                    _invalidIndexes.Remove(index);
                    item = Items[index];

                    if (_lastValidIndex < index)
                        _lastValidIndex = index;
                }

                return true;
            }

            public void Remove(T item)
            {
                item.ServiceObjectId.OnPreRemove();

                lock (_itemsToRemoveLock)
                {
                    _itemsToRemove.Push(item);
                }
            }

            public int Update()
            {
                lock (_updateLock)
                {
                    lock (_itemsToRemoveLock)
                    {
                        while (_itemsToRemove.Count > 0)
                        {
                            T item = _itemsToRemove.Pop();
                            ServiceObjectId id = item.ServiceObjectId;

                            if (id.IsPendingRemoval && id.IsSet)
                                RemoveInternal(id.Value);
                        }
                    }

                    while (_lastValidIndex > -1)
                    {
                        if (Items[_lastValidIndex]?.ServiceObjectId.IsSet == true)
                            break;

                        _lastValidIndex--;
                    }

                    lock (_itemsToAddLock)
                    {
                        while (_itemsToAdd.Count > 0)
                        {
                            T item = _itemsToAdd.Pop();
                            ServiceObjectId id = item.ServiceObjectId;

                            if (id.IsPendingAddition && !id.IsSet)
                                id.Value = AddInternal(item);
                        }
                    }
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
                Action cleanUpForReuseAction = serviceObjectId.CleanupForReuseAction;

                if (cleanUpForReuseAction == null)
                    Items[id] = null;
                else
                    cleanUpForReuseAction();
            }
        }

        private interface IServiceObjectArray { }
    }
}
