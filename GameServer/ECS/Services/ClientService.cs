using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.ServerProperties;
using ECS.Debug;
using log4net;

namespace DOL.GS
{
    public static class ClientService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ClientService);
        private const int PING_TIMEOUT = 60000;
        private const int HARD_TIMEOUT = 600000;

        private static List<GameClient> _clients = new();
        private static SimpleDisposableLock _lock = new();
        private static int _lastValidIndex;
        private static int _clientCount;

        public static int ClientCount => _clientCount; // `_clients` contains null objects.

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            using (_lock.GetWrite())
            {
                _clients = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out _lastValidIndex);
            }

            Parallel.For(0, _lastValidIndex + 1, i =>
            {
                GameClient client = _clients[i]; // Read lock unneeded.

                if (client?.EntityManagerId.IsSet != true)
                    return;

                switch (client.ClientState)
                {
                    case GameClient.eClientState.Disconnected:
                    {
                        OnClientDisconnect(client);
                        client.PacketProcessor?.OnDisconnect();
                        return;
                    }
                    case GameClient.eClientState.NotConnected:
                    case GameClient.eClientState.Linkdead:
                        return;
                    case GameClient.eClientState.CharScreen:
                    {
                        CheckPingTimeout(client);
                        break;
                    }
                    case GameClient.eClientState.Playing:
                    {
                        CheckPingTimeout(client);
                        GamePlayer player = client.Player;

                        if (player?.ObjectState == GameObject.eObjectState.Active)
                        {
                            try
                            {
                                if (player.LastWorldUpdate + Properties.WORLD_PLAYER_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                                {
                                    long startTick = GameLoop.GetCurrentTime();
                                    UpdateWorld(player);
                                    long stopTick = GameLoop.GetCurrentTime();

                                    if (stopTick - startTick > 25)
                                        log.Warn($"Long {SERVICE_NAME}.{nameof(UpdateWorld)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
                                }

                                player.movementComponent.Tick(GameLoop.GameLoopTime);
                            }
                            catch (Exception e)
                            {
                                ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, player);
                            }
                        }

                        break;
                    }
                    default:
                    {
                        CheckHardTimeout(client);
                        break;
                    }
                }

                try
                {
                    long startTick = GameLoop.GetCurrentTime();
                    client.PacketProcessor.ProcessTcpQueue();
                    long stopTick = GameLoop.GetCurrentTime();

                    if (stopTick - startTick > 25)
                        log.Warn($"Long {SERVICE_NAME}.{nameof(client.PacketProcessor.ProcessTcpQueue)} for {client.Account.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, client.Player);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static void OnClientConnect(GameClient client)
        {
            EntityManager.Add(client);
            Interlocked.Increment(ref _clientCount);
        }

        public static void OnClientDisconnect(GameClient client)
        {
            Interlocked.Decrement(ref _clientCount);
            EntityManager.Remove(client);
        }

        public static GamePlayer GetPlayer<T>(CheckPlayerAction<T> action)
        {
            return GetPlayer(action, default);
        }

        public static GamePlayer GetPlayer<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            using (_lock.GetRead())
            {
                foreach (GameClient client in _clients)
                {
                    if (client == null || !client.IsPlaying)
                        continue;

                    GamePlayer player = client.Player;

                    if (action?.Invoke(player, actionArgument) != false)
                        return player;
                }
            }

            return null;
        }

        public static List<GamePlayer> GetPlayers()
        {
            return GetPlayers<object>(null, null);
        }

        public static List<GamePlayer> GetPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetPlayers(action, default);
        }

        public static List<GamePlayer> GetPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            List<GamePlayer> players = new();

            using (_lock.GetRead())
            {
                foreach (GameClient client in _clients)
                {
                    if (client == null || !client.IsPlaying)
                        continue;

                    GamePlayer player = client.Player;

                    if (action?.Invoke(player, actionArgument) != false)
                        players.Add(player);
                }
            }

            return players;
        }

        public static GameClient GetClient<T>(CheckClientAction<T> action)
        {
            return GetClient(action, default);
        }

        public static GameClient GetClient<T>(CheckClientAction<T> action, T actionArgument)
        {
            using (_lock.GetRead())
            {
                foreach (GameClient client in _clients)
                {
                    if (client?.Account == null)
                        continue;

                    if (action?.Invoke(client, actionArgument) != false)
                        return client;
                }
            }

            return null;
        }

        public static List<GameClient> GetClients()
        {
            return GetClients<object>(null, null);
        }

        public static List<GameClient> GetClients<T>(CheckClientAction<T> action)
        {
            return GetClients(action, default);
        }

        public static List<GameClient> GetClients<T>(CheckClientAction<T> action, T actionArgument)
        {
            List<GameClient> players = new();

            using (_lock.GetRead())
            {
                foreach (GameClient client in _clients)
                {
                    if (client?.Account == null)
                        continue;

                    if (action?.Invoke(client, actionArgument) != false)
                        players.Add(client);
                }
            }

            return players;
        }

        public static GamePlayer GetPlayerByExactName(string playerName)
        {
            return GetPlayer(Predicate, playerName);

            static bool Predicate(GamePlayer player, string playerName)
            {
                if (!player.Client.IsPlaying || player.ObjectState != GameObject.eObjectState.Active)
                    return false;

                if (player.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase))
                    return true;

                return false;
            }
        }

        public static GamePlayer GetPlayerByPartialName(string playerName, out PlayerGuessResult result)
        {
            List<GamePlayer> partialMatches = new();
            GamePlayer targetPlayer = GetPlayer(Predicate, (playerName, partialMatches));

            if (targetPlayer != null)
                result = PlayerGuessResult.FOUND_EXACT;
            else if (partialMatches.Count < 1)
                result = PlayerGuessResult.NOT_FOUND;
            else if (partialMatches.Count > 1)
                result = PlayerGuessResult.FOUND_MULTIPLE;
            else
            {
                result = PlayerGuessResult.FOUND_PARTIAL;
                targetPlayer = partialMatches[0];
            }

            return targetPlayer;

            static bool Predicate(GamePlayer player, (string playerName, List<GamePlayer> partialList) args)
            {
                if (!player.Client.IsPlaying || player.ObjectState != GameObject.eObjectState.Active)
                    return false;

                if (player.Name.Equals(args.playerName, StringComparison.OrdinalIgnoreCase))
                    return true;

                if (player.Name.StartsWith(args.playerName, StringComparison.OrdinalIgnoreCase))
                    args.partialList.Add(player);

                return false;
            }
        }

        public static List<GamePlayer> GetNonGmPlayers()
        {
            return GetPlayers<object>(Predicate, default);

            static bool Predicate(GamePlayer player, object unused)
            {
                return player.Client.Account.PrivLevel == (uint) ePrivLevel.Player;
            }
        }

        public static List<GamePlayer> GetGmPlayers()
        {
            return GetPlayers<object>(Predicate, default);

            static bool Predicate(GamePlayer player, object unused)
            {
                return player.Client.Account.PrivLevel > (uint) ePrivLevel.Player;
            }
        }

        public static List<GamePlayer> GetPlayersOfRealm(eRealm realm)
        {
            return GetPlayersOfRealm<object>((realm, default, default));
        }

        public static List<GamePlayer> GetPlayersOfRealm<T>(eRealm realm, CheckPlayerAction<T> action)
        {
            return GetPlayersOfRealm((realm, action, default));
        }

        public static List<GamePlayer> GetPlayersOfRealm<T>((eRealm, CheckPlayerAction<T>, T) args)
        {
            return GetPlayers(Predicate, args);

            static bool Predicate(GamePlayer player, (eRealm realm, CheckPlayerAction<T> action, T actionArgument) args)
            {
                return player.Realm == args.realm && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public static List<GamePlayer> GetPlayersOfRegion(Region region)
        {
            return GetPlayersOfRegion<object>((region, default, default));
        }

        public static List<GamePlayer> GetPlayersOfRegion<T>(Region region, CheckPlayerAction<T> action)
        {
            return GetPlayersOfRegion((region, action, default));
        }

        public static List<GamePlayer> GetPlayersOfRegion<T>((Region, CheckPlayerAction<T>, T) args)
        {
            return GetPlayers(Predicate, args);

            static bool Predicate(GamePlayer player, (Region region, CheckPlayerAction<T> action, T actionArgument) args)
            {
                return player.CurrentRegion == args.region && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public static List<GamePlayer> GetPlayersOfRegionAndRealm(Region region, eRealm realm)
        {
            return GetPlayersOfRegion((region, Predicate, realm));

            static bool Predicate(GamePlayer player, eRealm realm)
            {
                return player.Realm == realm;
            }
        }

        public static List<GamePlayer> GetPlayersOfZone(Zone zone)
        {
            return GetPlayers(Predicate, zone);

            static bool Predicate(GamePlayer player, Zone zone)
            {
                return player.CurrentZone == zone;
            }
        }

        // Advice, Broadcast, LFG, Trade.
        public static List<GamePlayer> GetPlayersForRealmWideChatMessage(GamePlayer sender)
        {
            return GetPlayers(Predicate, sender);

            static bool Predicate(GamePlayer player, GamePlayer sender)
            {
                return GameServer.ServerRules.IsAllowedToUnderstand(sender, player) && !player.IsIgnoring(sender);
            }
        }

        public static GameClient GetClientFromId(int id)
        {
            // Since we want to avoid locks, `_clients` may change, so we can't check for count first.
            // This should be fine unless for some reason a client keeps sending wrong IDs.
            try
            {
                using (_lock.GetRead())
                return _clients[id - 1];
            }
            catch
            {
                return null;
            }
        }

        public static GameClient GetClientFromAccount(DbAccount account)
        {
            return GetClient(Predicate, account);

            static bool Predicate(GameClient client, DbAccount account)
            {
                return client.Account != null && client.Account == account;
            }
        }

        public static GameClient GetClientFromAccountName(string accountName)
        {
            return GetClient(Predicate, accountName);

            static bool Predicate(GameClient client, string accountName)
            {
                return client.Account != null && client.Account.Name.Equals(accountName);
            }
        }

        public static GameClient GetClientWithSameIp(GameClient otherClient)
        {
            return GetClient(Predicate, otherClient);

            static bool Predicate(GameClient client, GameClient otherClient)
            {
                return client.Account != null && client.Account.PrivLevel <= (uint) ePrivLevel.Player && client.TcpEndpointAddress.Equals(otherClient.TcpEndpointAddress) && client != otherClient;
            }
        }

        public static int SavePlayers()
        {
            int savedCount = 0;

            using (_lock.GetRead())
            {
                Parallel.ForEach(_clients, client =>
                {
                    if (client == null)
                        return;

                    client.SavePlayer();
                    savedCount++;
                });
            }

            return savedCount;
        }

        private static void AddNpcToPlayerCache(GamePlayer player, GameNPC npc)
        {
            if (player.NpcUpdateCache.TryGetValue(npc, out CachedNpcValues cachedNpcValues))
            {
                cachedNpcValues.Time = GameLoop.GameLoopTime;
                cachedNpcValues.HealthPercent =  npc.HealthPercent;
            }
            else
                player.NpcUpdateCache[npc] = new CachedNpcValues(GameLoop.GameLoopTime, npc.HealthPercent);
        }

        private static void AddItemToPlayerCache(GamePlayer player, GameStaticItem item)
        {
            player.ItemUpdateCache[item] = GameLoop.GameLoopTime;
        }

        private static void AddDoorToPlayerCache(GamePlayer player, GameDoorBase door)
        {
            player.DoorUpdateCache[door] = GameLoop.GameLoopTime;
        }

        private static void AddHouseToPlayerCache(GamePlayer player, House house)
        {
            player.HouseUpdateCache[house] = GameLoop.GameLoopTime;
        }

        private static void AddObjectToPlayerCache(GamePlayer player, GameObject gameObject)
        {
            // Doesn't handle houses. They aren't 'GameObject'.
            switch (gameObject.GameObjectType)
            {
                case eGameObjectType.ITEM:
                {
                    AddItemToPlayerCache(player, gameObject as GameStaticItem);
                    break;
                }
                case eGameObjectType.NPC:
                {
                    AddNpcToPlayerCache(player, gameObject as GameNPC);
                    break;
                }
                case eGameObjectType.DOOR:
                {
                    AddDoorToPlayerCache(player, gameObject as GameDoorBase);
                    break;
                }
            }
        }

        public static void UpdateObjectForPlayer(GamePlayer player, GameObject gameObject)
        {
            // Doesn't handle houses. They aren't 'GameObject'.
            gameObject.OnUpdateByPlayerService();
            player.Out.SendObjectUpdate(gameObject);
            AddObjectToPlayerCache(player, gameObject);
        }

        public static void UpdateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                UpdateObjectForPlayer(player, gameObject);
        }

        public static void CreateObjectForPlayer(GamePlayer player, GameObject gameObject)
        {
            player.Out.SendObjectCreate(gameObject);
            AddObjectToPlayerCache(player, gameObject);
        }

        public static void CreateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                CreateObjectForPlayer(player, gameObject);
        }

        private static void CheckPingTimeout(GameClient client)
        {
            if (client.PingTime + PING_TIMEOUT < GameLoop.GetCurrentTime())
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Ping timeout for client {client}");

                GameServer.Instance.Disconnect(client);
            }
        }

        private static void CheckHardTimeout(GameClient client)
        {
            if (client.PingTime + HARD_TIMEOUT < GameLoop.GetCurrentTime())
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Hard timeout for client {client}");

                GameServer.Instance.Disconnect(client);
            }
        }

        private static void UpdateWorld(GamePlayer player)
        {
            // Players aren't updated here on purpose.
            UpdateNpcs(player);
            UpdateItems(player);
            UpdateDoors(player);
            UpdateHouses(player);
            player.LastWorldUpdate = GameLoop.GameLoopTime;
        }

        private static void UpdateNpcs(GamePlayer player)
        {
            ConcurrentDictionary<GameNPC, CachedNpcValues> npcUpdateCache = player.NpcUpdateCache;

            foreach (var npcInCache in npcUpdateCache)
            {
                GameNPC npc = npcInCache.Key;

                if (!npc.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || npc.ObjectState != GameObject.eObjectState.Active || !npc.IsVisibleTo(player))
                    npcUpdateCache.Remove(npc, out _);
            }

            List<GameNPC> npcsInRange = player.GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, WorldMgr.VISIBILITY_DISTANCE);
            GameObject targetObject = player.TargetObject;
            GameNPC pet = player.ControlledBrain?.Body;
            CachedNpcValues cachedTargetValues = null;
            CachedNpcValues cachedPetValues = null;

            foreach (GameNPC objectInRange in npcsInRange)
            {
                if (!objectInRange.IsVisibleTo(player))
                    continue;

                if (!npcUpdateCache.TryGetValue(objectInRange, out CachedNpcValues cachedNpcValues))
                    UpdateObjectForPlayer(player, objectInRange);
                else if (cachedNpcValues.Time + Properties.WORLD_NPC_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                    UpdateObjectForPlayer(player, objectInRange);
                else if (cachedNpcValues.Time + 250 < GameLoop.GameLoopTime)
                {
                    // `GameNPC.HealthPercent` is a bit of an expensive call. Do it last.
                    if (objectInRange == targetObject)
                    {
                        if (objectInRange.HealthPercent > cachedNpcValues.HealthPercent)
                            cachedTargetValues = cachedNpcValues;
                    }
                    else if (objectInRange == pet)
                    {
                        if (objectInRange.HealthPercent > cachedNpcValues.HealthPercent)
                            cachedPetValues = cachedNpcValues;
                    }
                }
            }

            if (cachedTargetValues != null)
                UpdateObjectForPlayer(player, targetObject);

            if (cachedPetValues != null)
                UpdateObjectForPlayer(player, pet);
        }

        private static void UpdateItems(GamePlayer player)
        {
            ConcurrentDictionary<GameStaticItem, long> itemUpdateCache = player.ItemUpdateCache;

            foreach (var itemInCache in itemUpdateCache)
            {
                GameStaticItem item = itemInCache.Key;

                if (!item.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || item.ObjectState != GameObject.eObjectState.Active || !item.IsVisibleTo(player))
                    itemUpdateCache.Remove(item, out _);
            }

            List<GameStaticItem> itemsInRange = player.GetObjectsInRadius<GameStaticItem>(eGameObjectType.ITEM, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GameStaticItem itemInRange in itemsInRange)
            {
                if (!itemInRange.IsVisibleTo(player))
                    continue;

                if (!itemUpdateCache.TryGetValue(itemInRange, out _))
                    CreateObjectForPlayer(player, itemInRange);
            }
        }

        private static void UpdateDoors(GamePlayer player)
        {
            ConcurrentDictionary<GameDoorBase, long> doorUpdateCache = player.DoorUpdateCache;

            foreach (var doorInCache in doorUpdateCache)
            {
                GameDoorBase door = doorInCache.Key;

                if (!door.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || door.ObjectState != GameObject.eObjectState.Active || !door.IsVisibleTo(player))
                    doorUpdateCache.Remove(door, out _);
            }

            List<GameDoorBase> doorsInRange = player.GetObjectsInRadius<GameDoorBase>(eGameObjectType.DOOR, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GameDoorBase doorInRange in doorsInRange)
            {
                if (!doorInRange.IsVisibleTo(player))
                    continue;

                if (!doorUpdateCache.TryGetValue(doorInRange, out long lastUpdate))
                {
                    CreateObjectForPlayer(player, doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange);
                }
                else if (lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                    UpdateObjectForPlayer(player, doorInRange);
            }
        }

        private static void UpdateHouses(GamePlayer player)
        {
            foreach (var houseEntry in player.HouseUpdateCache)
            {
                House house = houseEntry.Key;

                if (house.RegionID != player.CurrentRegionID || !house.IsWithinRadius(player, HousingConstants.HouseViewingDistance))
                    player.HouseUpdateCache.Remove(house, out _);
            }

            if (player.CurrentRegion == null || !player.CurrentRegion.HousingEnabled)
                return;

            ICollection<House> houses = HouseMgr.GetHouses(player.CurrentRegionID).Values;

            foreach (House house in houses)
            {
                if (!player.IsWithinRadius(house, HousingConstants.HouseViewingDistance))
                    continue;

                if (!player.HouseUpdateCache.TryGetValue(house, out long lastUpdate))
                {
                    player.Client.Out.SendHouse(house);
                    player.Client.Out.SendGarden(house);
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
                }
                else if (lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);

                AddHouseToPlayerCache(player, house);
            }
        }

        // Arguments are used to allow the use of more performant static delegates (avoids closures completely).
        public delegate bool CheckPlayerAction<T>(GamePlayer player, T argument);
        public delegate bool CheckClientAction<T>(GameClient client, T argument);

        public enum PlayerGuessResult
        {
            NOT_FOUND,
            FOUND_EXACT,
            FOUND_PARTIAL,
            FOUND_MULTIPLE
        }

        public class CachedNpcValues
        {
            public long Time { get; set; }
            public byte HealthPercent { get; set; }

            public CachedNpcValues(long time, byte healthPercent)
            {
                Time = time;
                HealthPercent = healthPercent;
            }
        }
    }
}
