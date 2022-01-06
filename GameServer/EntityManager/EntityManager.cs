using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Effects;

namespace DOL.GS

{
    public static class EntityManager
    {
        public static int maxEntities = ServerProperties.Properties.MAX_ENTITIES;
        public static int maxPlayers = ServerProperties.Properties.MAX_PLAYERS;
        
        private static List<GamePlayer> _players = new List<GamePlayer>(maxPlayers);
        private static object _playersLock = new object();

        private static GameLiving[] _npcsArray = new GameLiving[maxEntities];
        private static int? _npcsLastDeleted = null;
        private static int _npcsCount = 0;

        private static List<ECSGameEffect> _effects = new List<ECSGameEffect>(50000);
        private static object _effectsLock = new object();

        private static List<Type> _services = new List<Type>(100);
        private static object _servicesLock = new object();

        private static Dictionary<Type, HashSet<GameLiving>> _components = new Dictionary<Type, HashSet<GameLiving>>(5000);
        private static object _componentLock = new object();

        private static bool npcsIsDirty;

        public static void AddService(Type t)
        {
            lock (_servicesLock)
            {
                _services.Add(t);
            }
        }
        public static Type[] GetServices(Type t)
        {
            lock (_services)
            {
                return _services.ToArray();
            }
        }

        public static void AddComponent(Type t, GameLiving n)
        {
            lock (_componentLock)
            {
                if (_components.ContainsKey(t))
                {
                    _components[t].Add(n);
                }
                else
                {
                    _components.Add(t, new HashSet<GameLiving> { n });
                }
            }
        }

        public static GameLiving[] GetLivingByComponent(Type t)
        {
            lock (_components)
            {
                if (_components.TryGetValue(t, out var p))
                {
                    return p.ToArray();
                }
                else
                {
                    return new GameLiving[0];
                }
            }
        }

        public static void RemoveComponent(Type t, GameLiving n)
        {
            lock (_componentLock)
            {
                if (_components.TryGetValue(t, out var nl))
                {
                    nl.Remove(n);
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

        public static GameLiving[] GetAllNpcs()
        {
            lock (_npcsArray)
            {
                npcsIsDirty = false;
                return _npcsArray;
            }
        }
        public static ref GameLiving[] GetAllNpcsArrayRef()
        {
            lock (_npcsArray)
            {
                npcsIsDirty = false;
                return ref _npcsArray;
            }
        }

        public static int? GetSkip(this Array array)
        {
            return _npcsLastDeleted;
        }

        public static bool GetAllNpcsDirty()
        {
            lock (_npcsArray)
            {
                bool wasDirty = npcsIsDirty;
                if (npcsIsDirty)
                    npcsIsDirty = false;

                return wasDirty;
            }
        }

        public static int AddNpc(GameLiving o)
        {
            lock (_npcsArray)
            {
                if (_npcsLastDeleted == null)
                {
                    _npcsArray[_npcsCount] = o;
                    _npcsCount++;
                    npcsIsDirty = true;
                    return (_npcsCount - 1);
                }
                else
                {
                    int last_id = (int)_npcsLastDeleted;
                    _npcsArray[(int)_npcsLastDeleted] = o;
                    _npcsLastDeleted = null;
                    npcsIsDirty = true;
                    return last_id;
                }
            }
        }

        public static void RemoveNpc(GameLiving o)
        {
            lock (_npcsArray)
            {
                _npcsArray[o.id] = null;
                _npcsLastDeleted = o.id;
                npcsIsDirty = true;
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
