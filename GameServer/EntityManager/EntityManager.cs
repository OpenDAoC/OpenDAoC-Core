using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    public static class EntityManager
    {
        public enum EntityType
        {
            Npc,
            Effect
        }

        private static Dictionary<EntityType, dynamic> Entities = new()
        {
            { EntityType.Npc, new EntityArrayWrapper<GameNPC>(ServerProperties.Properties.MAX_ENTITIES) },
            { EntityType.Effect, new EntityArrayWrapper<ECSGameEffect>(50000) }
        };

        private static List<GamePlayer> _players = new(ServerProperties.Properties.MAX_PLAYERS);
        private static object _playersLock = new();

        private static List<Type> _services = new(100);
        private static object _servicesLock = new();

        private static ConcurrentDictionary<Type, HashSet<GameLiving>> _components = new();

        public static int Add<T>(EntityType type, T entity)
        {
            return Entities[type].Add(entity);
        }

        public static void Remove(EntityType type, int id)
        {
            Entities[type].Remove(id);
        }

        public static T[] GetAll<T>(EntityType type)
        {
            return Entities[type].Elements;
        }

        public static int GetLastNonNullIndex(EntityType type)
        {
            return Entities[type].LastNonNullIndex;
        }

        public static void AddService(Type t)
        {
            lock (_servicesLock)
            {
                _services.Add(t);
            }
        }

        public static void AddComponent(Type t, GameLiving n)
        {
            if (_components.TryGetValue(t, out var p))
            {
                lock(p)
                {
                    p.Add(n);
                }
            }
            else
                _components.TryAdd(t, new HashSet<GameLiving> { n });
        }

        public static GameLiving[] GetLivingByComponent(Type t)
        {
            if (_components.TryGetValue(t, out var p))
            {
                lock(p)
                {
                    return p.ToArray();
                }
            }
            else
                return Array.Empty<GameLiving>();
        }

        public static void RemoveComponent(Type t, GameLiving n)
        {
            if (_components.TryGetValue(t, out var p))
            {
                lock(p)
                {
                    p.Remove(n);
                }
            }
        }

        public static GamePlayer[] GetAllPlayers()
        {
            lock (_players)
            {
                return _players.ToArray();
            }
        }

        public static void AddPlayer(GamePlayer p)
        {
            lock (_playersLock)
            {
                _players.Add(p);
            }
        }

        public static void RemovePlayer(GamePlayer p)
        {
            lock (_playersLock)
            {
                _players.Remove(p);
            }
        }

        private class EntityArrayWrapper<T> where T : class
        {
            private static Comparer<int> _descendingOrder = Comparer<int>.Create((x, y) => x < y ? 1 : x > y ? -1 : 0);
            public T[] Elements { get; private set; }
            public int LastNonNullIndex { get; private set; } = -1;
            private SortedSet<int> _deletedIndexes = new(_descendingOrder);
            private object _lock = new();

            public EntityArrayWrapper(int count)
            {
                Elements = new T[count];
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
