using System;
using System.Collections.Generic;
using System.Linq;
using DOL.GS.Effects;

namespace DOL.GS

{
    public static class EntityManager
    {
        //todo - have server property with Max Player size?
        private static List<GamePlayer> _players = new List<GamePlayer>(4000);
        private static object _playersLock = new object();

        private static List<GameLiving> _npcs = new List<GameLiving>(50000);
        private static object _npcsLock = new object();

        private static List<ECSGameEffect> _effects = new List<ECSGameEffect>(50000);
        private static object _effectsLock = new object();

        private static List<Type> _services = new List<Type>(100);
        private static object _servicesLock = new object();

        private static Dictionary<Type, List<GamePlayer>> _playerComponents = new Dictionary<Type, List<GamePlayer>>(5000);
        private static object _playerComponentLock = new object();

        private static Dictionary<Type, List<GameLiving>> _npcComponents = new Dictionary<Type, List<GameLiving>>(5000);
        private static object _npcComponentLock = new object();

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

        public static void AddPlayerComponent(Type t, GamePlayer p)
        {
            lock (_playerComponentLock)
            {
                if (_playerComponents.ContainsKey(t))
                {
                    _playerComponents[t].Add(p);
                }
                else
                {
                    _playerComponents.Add(t, new List<GamePlayer> { p });
                }
            }
        }

        public static GamePlayer[] GetPlayersByComponent(Type t)
        {
            lock (_playerComponents)
            {
                if (_playerComponents.TryGetValue(t, out var p))
                {
                    return p.ToArray();
                }
                else
                {
                    return new GamePlayer[0];
                }
            }
        }

        public static void AddNpcComponent(Type t, GameLiving n)
        {
            lock (_npcComponentLock)
            {
                if (_npcComponents.ContainsKey(t))
                {
                    _npcComponents[t].Add(n);
                }
                else
                {
                    _npcComponents.Add(t, new List<GameLiving> { n });
                }
            }
        }

        public static GameLiving[] GetNpcsByComponent(Type t)
        {
            lock (_npcComponents)
            {
                if (_npcComponents.TryGetValue(t, out var p))
                {
                    return p.ToArray();
                }
                else
                {
                    return new GameLiving[0];
                }
            }
        }

        public static void RemovePlayerComponent(Type t, GamePlayer p)
        {
            lock (_playerComponentLock)
            {
                if (_playerComponents.TryGetValue(t, out var pl))
                {
                    pl.Remove(p);
                }
            }
        }

        public static void RemoveNpcComponent(Type t, GameLiving n)
        {
            lock (_npcComponentLock)
            {
                if (_npcComponents.TryGetValue(t, out var nl))
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
                Console.WriteLine($"Added {p.Name}");
                _players.Add(p);
            }
        }

        public static void RemovePlayer(GamePlayer p)
        {
            lock (_playersLock)
            {
                Console.WriteLine($"Removed {p.Name}");
                _players.Remove(p);
            }
        }

        public static GameLiving[] GetAllNpcs()
        {
            lock (_npcs)
            {
                return _npcs.ToArray();
            }
        }

        public static void AddNpc(GameLiving o)
        {
            lock (_npcsLock)
            {
                _npcs.Add(o);
            }
        }

        public static void RemoveNpc(GameLiving o)
        {
            lock (_npcsLock)
            {
                _npcs.Remove(o);
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