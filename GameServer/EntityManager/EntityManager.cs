using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace DOL.GS
{
    public static class EntityManager
    {
        private static List<GamePlayer> _players = new(ServerProperties.Properties.MAX_PLAYERS);
        private static object _playersLock = new();

        private static GameNPC[] _npcs = new GameNPC[ServerProperties.Properties.MAX_ENTITIES];
        private static SortedSet<int> _deletedNpcIndexes = new(Comparer<int>.Create((x, y) => x < y ? 1 : x > y ? -1 : 0)); // Reverse order.
        private static object _npcsLock = new();

        private static ECSGameEffect[] _effects = new ECSGameEffect[50000];
        private static SortedSet<int> _deletedEffectIndexes = new(Comparer<int>.Create((x, y) => x < y ? 1 : x > y ? -1 : 0)); // Reverse order.
        private static object _effectsLock = new();

        private static List<Type> _services = new(100);
        private static object _servicesLock = new();

        private static ConcurrentDictionary<Type, HashSet<GameLiving>> _components = new();

        public static int LastNonNullNpcIndex { get; private set; } = -1;
        public static int LastNonNullEffectIndex { get; private set; } = -1;

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

        public static GameNPC[] GetAllNpcs()
        {
            return _npcs;
        }

        public static int AddNpc(GameNPC o)
        {
            lock (_npcsLock)
            {
                if (_deletedNpcIndexes.Any())
                {
                    int index = _deletedNpcIndexes.Max;
                    _deletedNpcIndexes.Remove(index);
                    _npcs[index] = o;

                    if (index > LastNonNullNpcIndex)
                        LastNonNullNpcIndex = index;

                    return index;
                }
                else
                {
                    LastNonNullNpcIndex++;
                    _npcs[LastNonNullNpcIndex] = o;
                    return LastNonNullNpcIndex;
                }
            }
        }

        public static void RemoveNpc(GameNPC o)
        {
            int id = o.EntityManagerId;

            if (id == -1)
                return;

            lock (_npcsLock)
            {
                _npcs[id] = null;
                _deletedNpcIndexes.Add(id);

                if (id == LastNonNullNpcIndex)
                {
                    if (_deletedNpcIndexes.Any())
                    {
                        int lastIndex = _deletedNpcIndexes.Min;

                        // Find the first non-contiguous number. For example if the collection contains 7 6 3 1, we should return 5.
                        foreach (int index in _deletedNpcIndexes)
                        {
                            if (lastIndex - index > 0)
                                break;

                            lastIndex--;
                        }

                        LastNonNullNpcIndex = lastIndex;
                    }
                    else
                        LastNonNullNpcIndex--;
                }
            }
        }

        public static ECSGameEffect[] GetAllEffects()
        {
            return _effects;
        }

        public static int AddEffect(ECSGameEffect e)
        {
            lock (_effectsLock)
            {
                if (_deletedEffectIndexes.Any())
                {
                    int index = _deletedEffectIndexes.Max;
                    _deletedEffectIndexes.Remove(index);
                    _effects[index] = e;

                    if (index > LastNonNullEffectIndex)
                        LastNonNullEffectIndex = index;

                    return index;
                }
                else
                {
                    LastNonNullEffectIndex++;
                    _effects[LastNonNullEffectIndex] = e;
                    return LastNonNullEffectIndex;
                }
            }
        }

        public static void RemoveEffect(ECSGameEffect e)
        {
            int id = e.EntityManagerId;

            if (id == -1)
                return;

            lock (_effectsLock)
            {
                _effects[id] = null;
                _deletedEffectIndexes.Add(id);

                if (id == LastNonNullEffectIndex)
                {
                    if (_deletedEffectIndexes.Any())
                    {
                        int lastIndex = _deletedEffectIndexes.Min;

                        // Find the first non-contiguous number. For example if the collection contains 7 6 3 1, we should return 5.
                        foreach (int index in _deletedEffectIndexes)
                        {
                            if (lastIndex - index > 0)
                                break;

                            lastIndex--;
                        }

                        LastNonNullEffectIndex = lastIndex;
                    }
                    else
                        LastNonNullEffectIndex--;
                }
            }
        }
    }
}
