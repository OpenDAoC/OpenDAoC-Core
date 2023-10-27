using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Core.GS.AI;
using Core.GS.Enums;
using Core.GS.Server;
using log4net;

namespace Core.GS.ECS
{
    public static class EntityMgr
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private static Dictionary<EEntityType, object> _entityArrays = new()
        {
            { EEntityType.Client, new EntityArray<GameClient>(ServerProperty.MAX_PLAYERS) },
            { EEntityType.Brain, new EntityArray<ABrain>(ServerProperty.MAX_ENTITIES) },
            { EEntityType.Effect, new EntityArray<EcsGameEffect>(250) },
            { EEntityType.AttackComponent, new EntityArray<AttackComponent>(1250) },
            { EEntityType.CastingComponent, new EntityArray<CastingComponent>(1250) },
            { EEntityType.EffectListComponent, new EntityArray<EffectListComponent>(3000) },
            { EEntityType.CraftComponent, new EntityArray<CraftComponent>(100) },
            { EEntityType.ObjectChangingSubZone, new EntityArray<ObjectChangingSubZone>(ServerProperty.MAX_ENTITIES) },
            { EEntityType.LivingBeingKilled, new EntityArray<LivingBeingKilled>(200) },
            { EEntityType.Timer, new EntityArray<EcsGameTimer>(500) },
            { EEntityType.AuxTimer, new EntityArray<AuxEcsGameTimer>(500) }
        };

        public static bool Add<T>(T entity) where T : class, IManagedEntity
        {
            EntityManagerId id = entity.EntityManagerId;

            // Return false if the entity is already present and not being removed.
            if (id.IsSet && !id.IsPendingRemoval)
                return false;

            (_entityArrays[entity.EntityManagerId.Type] as EntityArray<T>).Add(entity);
            return true;
        }

        public static bool TryReuse<T>(EEntityType type, out T entity) where T : class, IManagedEntity
        {
            return (_entityArrays[type] as EntityArray<T>).TryReuse(out entity);
        }

        public static bool Remove<T>(T entity) where T : class, IManagedEntity
        {
            EntityManagerId id = entity.EntityManagerId;

            // Return false if the entity is absent and not being added.
            if (!id.IsSet && !id.IsPendingAddition)
                return false;

            (_entityArrays[entity.EntityManagerId.Type] as EntityArray<T>).Remove(entity);
            return true;
        }

        // Applies pending additions and removals then returns the list alongside the last valid index.
        // Thread unsafe. The returned list should not be modified.
        public static List<T> UpdateAndGetAll<T>(EEntityType type, out int lastValidIndex) where T : IManagedEntity
        {
            dynamic array = _entityArrays[type];
            lastValidIndex = array.Update();
            return array.Entities;
        }

        private class EntityArray<T> where T : class, IManagedEntity
        {
            private SortedSet<int> _invalidIndexes = new();
            private Stack<T> _entitiesToAdd  = new();
            private Stack<T> _entitiesToRemove = new();
            private object _updateLock = new();
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

                lock (_updateLock)
                {
                    if (_invalidIndexes.Count == 0)
                        return false;

                    index = _invalidIndexes.Min;
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
                lock (_updateLock)
                {
                    lock (_entitiesToRemoveLock)
                    {
                        while (_entitiesToRemove.Count > 0)
                        {
                            T entity = _entitiesToRemove.Pop();
                            EntityManagerId id = entity.EntityManagerId;

                            if (id.IsPendingRemoval && id.IsSet)
                                RemoveInternal(id.Value);
                        }
                    }

                    while (_lastValidIndex > -1)
                    {
                        if (Entities[_lastValidIndex]?.EntityManagerId.IsSet == true)
                            break;

                        _lastValidIndex--;
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
                }

                return _lastValidIndex;
            }

            private int AddInternal(T entity)
            {
                if (_invalidIndexes.Count > 0)
                {
                    int index = _invalidIndexes.Min;
                    _invalidIndexes.Remove(index);
                    Entities[index] = entity;

                    if (index > _lastValidIndex)
                        _lastValidIndex = index;

                    return index;
                }

                // Increase the capacity of the list in the event that it's too small. This is a costly operation.
                // 'Add' already does it, but we nay want to know when it happens and control by how much it grows ('Add' would double it).
                if (++_lastValidIndex >= Entities.Capacity)
                {
                    int newCapacity = (int) (Entities.Capacity * 1.2);

                    if (log.IsWarnEnabled)
                        log.Warn($"{typeof(T)} {nameof(Entities)} is too short. Resizing it to {newCapacity}.");

                    ListExtras.Resize(Entities, newCapacity);
                }

                Entities.Add(entity);
                return _lastValidIndex;
            }

            private void RemoveInternal(int id)
            {
                T entity = Entities[id];

                if (id == Entities.Count)
                    _lastValidIndex--;

                EntityManagerId entityManagerId = entity.EntityManagerId;
                entityManagerId.Unset();
                _invalidIndexes.Add(id);

                if (!entityManagerId.AllowReuseByEntityManager)
                    Entities[id] = null;
            }
        }
    }

    public class EntityManagerId
    {
        private const int UNSET_ID = -1;
        private int _value = UNSET_ID;
        private EPendingState _pendingState = EPendingState.NONE;

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _pendingState = EPendingState.NONE;
            }
        }
        public EEntityType Type { get; private set; }
        public bool AllowReuseByEntityManager { get; private set; }
        public bool IsSet => _value > UNSET_ID;
        public bool IsPendingAddition => _pendingState == EPendingState.ADDITION;
        public bool IsPendingRemoval => _pendingState == EPendingState.REMOVAL;

        public EntityManagerId(EEntityType type, bool allowReuseByEntityManager)
        {
            Type = type;
            AllowReuseByEntityManager = allowReuseByEntityManager;
        }

        public void OnPreAdd()
        {
            _pendingState = EPendingState.ADDITION;
        }

        public void OnPreRemove()
        {
            _pendingState = EPendingState.REMOVAL;
        }

        public void Unset()
        {
            Value = UNSET_ID;
        }

        private enum EPendingState
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