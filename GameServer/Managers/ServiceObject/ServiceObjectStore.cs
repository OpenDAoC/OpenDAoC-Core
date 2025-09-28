using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.AI;

namespace DOL.GS
{
    public static class ServiceObjectStore
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private static FrozenDictionary<ServiceObjectType, IServiceObjectArray> _serviceObjectArrays =
            new Dictionary<ServiceObjectType, IServiceObjectArray>()
            {
                { ServiceObjectType.Client, new ServiceObjectArray<GameClient>(ServerProperties.Properties.MAX_PLAYERS) },
                { ServiceObjectType.Brain, new ServiceObjectArray<ABrain>(ServerProperties.Properties.MAX_ENTITIES) },
                { ServiceObjectType.AttackComponent, new ServiceObjectArray<AttackComponent>(1250) },
                { ServiceObjectType.CastingComponent, new ServiceObjectArray<CastingComponent>(1250) },
                { ServiceObjectType.Effect, new ServiceObjectArray<ECSGameEffect>(10000) },
                { ServiceObjectType.EffectListComponent, new ServiceObjectArray<EffectListComponent>(1250) },
                { ServiceObjectType.MovementComponent, new ServiceObjectArray<MovementComponent>(1250) },
                { ServiceObjectType.CraftComponent, new ServiceObjectArray<CraftComponent>(100) },
                { ServiceObjectType.SubZoneObject, new ServiceObjectArray<SubZoneObject>(ServerProperties.Properties.MAX_ENTITIES) },
                { ServiceObjectType.LivingBeingKilled, new ServiceObjectArray<LivingBeingKilled>(200) },
                { ServiceObjectType.Timer, new ServiceObjectArray<ECSGameTimer>(500) }
            }.ToFrozenDictionary();

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
            private PriorityQueue<int, int> _invalidIndexes = new();
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
                    throw new InvalidOperationException($"{typeof(T)} is already being updated.");

                try
                {
                    if (_itemsToRemove.Any)
                    {
                        DrainItemsToRemove();
                        UpdateLastValidIndexAfterRemoval();
                        OptimizeIndexes();
                    }

                    if (_itemsToAdd.Any)
                        DrainItemsToAdd();
                }
                finally
                {
                    _updating = 0;
                }

                return _lastValidIndex;
            }

            private void DrainItemsToRemove()
            {
                _itemsToRemove.DrainTo(static (item, array) => array.RemoveInternal(item), this);
            }

            private void RemoveInternal(T item)
            {
                ServiceObjectId id = item.ServiceObjectId;

                if (!id.IsPendingRemoval || !id.IsSet)
                    return;

                int idValue = id.Value;

                if (idValue == Items.Count - 1)
                    _lastValidIndex--;

                _invalidIndexes.Enqueue(idValue, idValue);
                Items[idValue] = null;
                id.Unset();
            }

            private void DrainItemsToAdd()
            {
                _itemsToAdd.DrainTo(static (item, array) => array.AddInternal(item), this);
            }

            private void AddInternal(T item)
            {
                ServiceObjectId id = item.ServiceObjectId;

                if (!id.IsPendingAddition || id.IsSet)
                    return;

                if (_invalidIndexes.Count > 0)
                {
                    int index = _invalidIndexes.Peek();
                    _invalidIndexes.Dequeue();
                    Items[index] = item;

                    if (index > _lastValidIndex)
                        _lastValidIndex = index;

                    id.Value = index;
                    return;
                }

                // Increase the capacity of the list in the event that it's too small. This is a costly operation.
                // 'Add' already does it, but we nay want to know when it happens and control by how much it grows ('Add' would double it).
                if (++_lastValidIndex >= Items.Capacity)
                {
                    int newCapacity = (int) (Items.Capacity * 1.2);

                    if (log.IsWarnEnabled)
                        log.Warn($"Array for type '{typeof(T)}' is too short. Resizing it to {newCapacity}.");

                    Items.Resize(newCapacity);
                }

                Items.Add(item);
                id.Value = _lastValidIndex;
            }

            private void UpdateLastValidIndexAfterRemoval()
            {
                while (_lastValidIndex > -1 && Items[_lastValidIndex]?.ServiceObjectId.IsSet != true)
                    _lastValidIndex--;
            }

            private void OptimizeIndexes()
            {
                // Only compact if there are invalid indexes and at least one valid item above the lowest invalid index.
                while (_invalidIndexes.Count > 0)
                {
                    int lowestInvalidIndex = _invalidIndexes.Peek();
                    bool foundItemToMove = false;

                    for (int i = _lastValidIndex; i > lowestInvalidIndex; i--)
                    {
                        if (Items[i]?.ServiceObjectId.IsSet != true)
                            continue;

                        _invalidIndexes.Dequeue();
                        T item = Items[i];
                        Items[lowestInvalidIndex] = item;
                        Items[i] = null;
                        _invalidIndexes.Enqueue(i, i);
                        item.ServiceObjectId.Value = lowestInvalidIndex;

                        // Update last valid index if we just moved the last item.
                        if (i == _lastValidIndex)
                        {
                            do
                                _lastValidIndex--;
                            while (_lastValidIndex > -1 && Items[_lastValidIndex]?.ServiceObjectId.IsSet != true);
                        }

                        foundItemToMove = true;
                        break;
                    }

                    if (!foundItemToMove)
                        break;
                }
            }
        }

        private interface IServiceObjectArray { }
    }
}
