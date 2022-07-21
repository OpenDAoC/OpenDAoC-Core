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
        private static int _nextNPCIndex = 0;

        private static List<ECSGameEffect> _effects = new List<ECSGameEffect>(50000);
        private static object _effectsLock = new object();

        private static List<Type> _services = new List<Type>(100);
        private static object _servicesLock = new object();

        private static Dictionary<Type, HashSet<GameLiving>> _components = new Dictionary<Type, HashSet<GameLiving>>(5000);
        private static object _componentLock = new object();

        private static Queue<int> IDQueue = new Queue<int>(maxEntities);

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
            lock (_componentLock)
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
            return _nextNPCIndex;
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
                //grab and ID from the queue if one is available
                if (IDQueue.Count > 0)
                {
                    var ID = IDQueue.Dequeue();
                    _npcsArray[ID] = o;
                    npcsIsDirty = true;
                    //Console.WriteLine($"Adding NPC {o.Name} with ID {ID} from queue");
                    return ID;
                }
                //if no free ID is available, we add a new one
                else
                {
                    int newID = (int)_nextNPCIndex;
                    
                    //if our array of entities is not big enough to accept a new entity
                    //double the array size and then add it
                    //NOTE: copying this array is an expensive operation, but it should happen infrequently enough to not be an issue
                    if (newID > _npcsArray.Length - 1)
                    {
                        GameLiving[] newArray = new GameLiving[_npcsArray.Length * 2];
                        //Console.WriteLine($"NPC Array too short, doubling from {_npcsArray.Length} to {newArray.Length}");
                        _npcsArray.CopyTo(newArray, 0);
                        _npcsArray = newArray;
                    }
                    
                    //Console.WriteLine($"Adding NPC {o.Name} with new ID {newID}");
                    //set array here
                    _npcsArray[(int)_nextNPCIndex] = o;
                    _nextNPCIndex++;
                    npcsIsDirty = true;
                    return newID;
                }
            }
        }

        public static void RemoveNpc(GameLiving o)
        {
            lock (_npcsArray)
            {
                _npcsArray[o.id] = null;
                //Console.WriteLine($"Removing NPC {o.Name} with ID {o.id} npcarraylength {_npcsArray.Length}");
                //return our ID to the queue to be re-used by something else
                IDQueue.Enqueue(o.id); 
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
