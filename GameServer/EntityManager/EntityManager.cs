using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DOL.AI;
using log4net;

namespace DOL.GS
{
    public static class EntityManager
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public enum EntityType
        {
            Player,
            Brain,
            Effect,
            AttackComponent,
            CastingComponent,
            EffectListComponent,
            CraftComponent,
            ObjectChangingSubZone,
            Timer,
            AuxTimer
        }

        private static Dictionary<EntityType, dynamic> _entityArrays = new()
        {
            { EntityType.Player, new EntityArray<GamePlayer>(ServerProperties.Properties.MAX_PLAYERS) },
            { EntityType.Brain, new EntityArray<ABrain>(ServerProperties.Properties.MAX_ENTITIES) },
            { EntityType.Effect, new EntityArray<ECSGameEffect>(250) },
            { EntityType.AttackComponent, new EntityArray<AttackComponent>(1250) },
            { EntityType.CastingComponent, new EntityArray<CastingComponent>(1250) },
            { EntityType.EffectListComponent, new EntityArray<EffectListComponent>(3000) },
            { EntityType.CraftComponent, new EntityArray<CraftComponent>(100) },
            { EntityType.ObjectChangingSubZone, new EntityArray<ObjectChangingSubZone>(ServerProperties.Properties.MAX_ENTITIES) },
            { EntityType.Timer, new EntityArray<ECSGameTimer>(500) },
            { EntityType.AuxTimer, new EntityArray<AuxECSGameTimer>(250) }
        };

        public static bool Add<T>(EntityType type, T entity) where T : IManagedEntity
        {
            EntityManagerId id = entity.EntityManagerId;

            // Return false if the entity is already present and not being removed.
            if (id.IsSet && !id.IsPendingRemoval)
                return false;

            _entityArrays[type].Add(entity);
            return true;
        }

        public static bool TryReuse<T>(EntityType type, out T entity) where T : IManagedEntity
        {
            return _entityArrays[type].TryReuse(out entity);
        }

        public static bool Remove<T>(EntityType type, T entity) where T : IManagedEntity
        {
            EntityManagerId id = entity.EntityManagerId;

            // Return false if the entity is absent and not being added.
            if (!id.IsSet && !id.IsPendingAddition)
                return false;

            _entityArrays[type].Remove(entity);
            return true;
        }

        // Applies pending additions and removals then returns the list alongside the last non-null index.
        // Thread unsafe. The returned list should not be modified.
        public static List<T> UpdateAndGetAll<T>(EntityType type, out int lastValidIndex) where T : IManagedEntity
        {
            dynamic array = _entityArrays[type];
            lastValidIndex = array.Update();
            return array.Entities;
        }

        private class EntityArray<T> where T : class, IManagedEntity
        {
            private static Comparer<int> _descendingOrder = Comparer<int>.Create((x, y) => x < y ? 1 : x > y ? -1 : 0);

            private SortedSet<int> _invalidIndexes = new(_descendingOrder);
            private Stack<T> _entitiesToAdd  = new();
            private Stack<T> _entitiesToRemove = new();
            private object _invalidIndexesLock = new();
            private object _entitiesToAddLock = new();
            private object _entitiesToRemoveLock = new();
            private int _lastValidIndex = -1;

            public List<T> Entities { get; private set; }

            public EntityArray(int capacity)
            {
                Entities = new List<T>(capacity);
            }

            public void Add(T entity)
            {
                entity.EntityManagerId.OnPreAdd();

                lock (_entitiesToAddLock)
                {
                    _entitiesToAdd.Push(entity);
                }
            }

            public bool TryReuse(out T entity)
            {
                int index;
                entity = null;

                lock (_invalidIndexesLock)
                {
                    if (!_invalidIndexes.Any())
                        return false;

                    index = _invalidIndexes.Max;
                    _invalidIndexes.Remove(index);
                    entity = Entities[index];
                    entity.EntityManagerId.Value = index;

                    if (_lastValidIndex < index)
                        _lastValidIndex = index;
                }

                return true;
            }

            public void Remove(T entity)
            {
                entity.EntityManagerId.OnPreRemove();

                lock (_entitiesToRemoveLock)
                {
                    _entitiesToRemove.Push(entity);
                }
            }

            public int Update()
            {
                lock (_entitiesToRemoveLock)
                {
                    while (_entitiesToRemove.Count > 0)
                    {
                        T entity = _entitiesToRemove.Pop();
                        EntityManagerId id = entity.EntityManagerId;

                        if (id.IsPendingRemoval)
                        {
                            if (id.IsSet)
                                RemoveInternal(id.Value);

                            id.Unset();
                        }
                    }
                }

                lock (_entitiesToAddLock)
                {
                    while (_entitiesToAdd.Count > 0)
                    {
                        T entity = _entitiesToAdd.Pop();
                        EntityManagerId id = entity.EntityManagerId;

                        if (id.IsPendingAddition && !id.IsSet)
                            id.Value = AddInternal(entity);
                    }
                }

                return _lastValidIndex;
            }

            private int AddInternal(T entity)
            {
                lock (_invalidIndexesLock)
                {
                    if (_invalidIndexes.Any())
                    {
                        int index = _invalidIndexes.Max;
                        _invalidIndexes.Remove(index);
                        Entities[index] = entity;

                        if (index > _lastValidIndex)
                            _lastValidIndex = index;

                        return index;
                    }

                    // Increase the capacity of the list in the event that it's too small. This is a costly operation.
                    // 'Add' already does it, but we want to know when it happens and control by how much it grows (instead of doubling it).
                    if (++_lastValidIndex >= Entities.Capacity)
                    {
                        int newCapacity = Entities.Capacity + 100;
                        log.Warn($"{typeof(T)} {nameof(Entities)} is too short. Resizing it to {newCapacity}.");
                        ListExtras.Resize(Entities, newCapacity);
                    }
                }

                Entities.Add(entity);
                return _lastValidIndex;
            }

            private void RemoveInternal(int id)
            {
                T entity = Entities[id];

                lock (_invalidIndexesLock)
                {
                    _invalidIndexes.Add(id);

                    if (!entity.AllowReuseByEntityManager)
                        Entities[id] = null;

                    if (id != _lastValidIndex)
                        return;

                    if (!_invalidIndexes.Any())
                    {
                        _lastValidIndex--;
                        return;
                    }

                    int lastIndex = _invalidIndexes.Min;

                    // Find the first non-contiguous number. For example if the collection contains 7 6 3 1, we should return 5.
                    foreach (int index in _invalidIndexes)
                    {
                        if (lastIndex - index > 0)
                            break;

                        lastIndex--;
                    }

                    _lastValidIndex = lastIndex;
                }
            }
        }
    }

    public class EntityManagerId
    {
        private const int UNSET_ID = -1;
        private int _value = UNSET_ID;
        private PendingState _pendingState = PendingState.NONE;

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _pendingState = PendingState.NONE;
            }
        }
        public bool IsSet => _value > UNSET_ID;
        public bool IsPendingAddition => _pendingState == PendingState.ADDITION;
        public bool IsPendingRemoval => _pendingState == PendingState.REMOVAL;

        public void OnPreAdd()
        {
            _pendingState = PendingState.ADDITION;
        }

        public void OnPreRemove()
        {
            _pendingState = PendingState.REMOVAL;
        }

        public void Unset()
        {
            Value = UNSET_ID;
        }

        private enum PendingState
        {
            NONE,
            ADDITION,
            REMOVAL
        }
    }

    // Interface to be implemented by classes that are to be managed by the entity manager.
    public interface IManagedEntity
    {
        public EntityManagerId EntityManagerId { get; set; }
        public bool AllowReuseByEntityManager { get; }
    }

    // Extension methods for 'List<T>' that could be moved elsewhere.
    public static class ListExtras
    {
        public static void Resize<T>(this List<T> list, int size, bool fill = false, T element = default)
        {
            int count = list.Count;

            if (size < count)
            {
                list.RemoveRange(size, count - size);
                list.TrimExcess();
            }
            else if (size > count)
            {
                if (size > list.Capacity)
                    list.Capacity = size; // Creates a new internal array.

                if (fill)
                    list.AddRange(Enumerable.Repeat(element, size - count));
            }
        }
    }
}
