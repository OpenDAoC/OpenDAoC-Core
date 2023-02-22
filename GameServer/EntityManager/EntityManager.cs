using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using log4net;

namespace DOL.GS
{
    public static class EntityManager
    {
        public const int UNSET_ID = -1;
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public enum EntityType
        {
            Player,
            Npc,
            Effect,
            AttackComponent,
            CastingComponent,
            EffectListComponent,
            CraftComponent
        }

        private static Dictionary<EntityType, dynamic> Entities = new()
        {
            { EntityType.Player, new EntityArrayWrapper<GamePlayer>(ServerProperties.Properties.MAX_PLAYERS) },
            { EntityType.Npc, new EntityArrayWrapper<GameNPC>(ServerProperties.Properties.MAX_ENTITIES) },
            { EntityType.Effect, new EntityArrayWrapper<ECSGameEffect>(250) },
            { EntityType.AttackComponent, new EntityArrayWrapper<AttackComponent>(1250) },
            { EntityType.CastingComponent, new EntityArrayWrapper<CastingComponent>(1250) },
            { EntityType.EffectListComponent, new EntityArrayWrapper<EffectListComponent>(3000) },
            { EntityType.CraftComponent, new EntityArrayWrapper<CraftComponent>(250) }
        };

        public static int Add<T>(EntityType type, T entity)
        {
            return Entities[type].Add(entity);
        }

        public static int Remove(EntityType type, int id)
        {
            Entities[type].Remove(id);
            return UNSET_ID;
        }

        public static T[] GetAll<T>(EntityType type)
        {
            return Entities[type].Elements;
        }

        public static int GetLastNonNullIndex(EntityType type)
        {
            return Entities[type].LastNonNullIndex;
        }

        private class EntityArrayWrapper<T> where T : class
        {
            private static Comparer<int> _descendingOrder = Comparer<int>.Create((x, y) => x < y ? 1 : x > y ? -1 : 0);
            public T[] Elements { get; private set; }
            public int LastNonNullIndex { get; private set; } = -1;
            private SortedSet<int> _deletedIndexes = new(_descendingOrder);
            private object _lock = new();

            public EntityArrayWrapper(int size)
            {
                Elements = new T[size];
            }

            public int Add(T element)
            {
                lock (_lock)
                {
                    if (_deletedIndexes.Any())
                    {
                        int index = _deletedIndexes.Max;
                        _deletedIndexes.Remove(index);
                        Elements[index] = element;

                        if (index > LastNonNullIndex)
                            LastNonNullIndex = index;

                        return index;
                    }
                    else
                    {
                        LastNonNullIndex++;

                        // Increase the size of the array in the unlikely event that it's too small. This is a costly operation.
                        if (LastNonNullIndex >= Elements.Length)
                        {
                            int newSize = Elements.Length + 100;

                            log.Warn($"{Elements.GetType()} {nameof(Elements)} is too short. Resizing it to {newSize}.");

                            T[] newArray = new T[newSize];
                            Elements.CopyTo(newArray, 0);
                            Elements = newArray;
                        }

                        Elements[LastNonNullIndex] = element;
                        return LastNonNullIndex;
                    }
                }
            }

            public void Remove(int id)
            {
                if (id == -1)
                    return;

                lock (_lock)
                {
                    Elements[id] = null;
                    _deletedIndexes.Add(id);

                    if (id == LastNonNullIndex)
                    {
                        if (_deletedIndexes.Any())
                        {
                            int lastIndex = _deletedIndexes.Min;

                            // Find the first non-contiguous number. For example if the collection contains 7 6 3 1, we should return 5.
                            foreach (int index in _deletedIndexes)
                            {
                                if (lastIndex - index > 0)
                                    break;

                                lastIndex--;
                            }

                            LastNonNullIndex = lastIndex;
                        }
                        else
                            LastNonNullIndex--;
                    }
                }
            }
        }
    }
}
