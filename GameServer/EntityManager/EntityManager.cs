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
        private static SortedSet<int> _deletedNpcIndexes = new();
        private static object _npcsLock = new();

        private static List<ECSGameEffect> _effects = new(50000);
        private static object _effectsLock = new();

        private static List<Type> _services = new(100);
        private static object _servicesLock = new();

        private static ConcurrentDictionary<Type, HashSet<GameLiving>> _components = new();

        public static int LastNonNullNpcIndex { get; private set; } = -1;

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
                    int index = _deletedNpcIndexes.Min;
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
            lock (_npcsLock)
            {
                _npcs[o.id] = null;

                if (o.id == LastNonNullNpcIndex)
                {
                    if (_deletedNpcIndexes.Any())
                        LastNonNullNpcIndex = _deletedNpcIndexes.Min - 1;
                    else
                        LastNonNullNpcIndex--;
                }
                else
                    _deletedNpcIndexes.Add(o.id);
            }
        }

        public static ECSGameEffect[] GetAllEffects()
        {
            lock (_effectsLock)
            {
                return _effects.ToArray();
            }
        }

        public static void AddEffect(ECSGameEffect e)
        {
            lock (_effectsLock)
            {
                _effects.Add(e);
            }
        }

        public static void RemoveEffect(ECSGameEffect e)
        {
            lock (_effectsLock)
            {
                _effects.Remove(e);
            }
        }
    }
}
