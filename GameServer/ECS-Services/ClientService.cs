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
        private const int POSITION_UPDATE_TIMEOUT = 5000;
        private const int STATIC_OBJECT_UPDATE_MIN_DISTANCE = 4000;

        private static List<GameClient> _clients = new();
        private static SimpleDisposableLock _lock = new(LockRecursionPolicy.SupportsRecursion);
        private static int _lastValidIndex;
        private static int _clientCount;

        public static int ClientCount => _clientCount; // `_clients` contains null objects.

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            using (_lock)
            {
                _lock.EnterWriteLock();
                _clients = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out _lastValidIndex);
            }

            Parallel.For(0, _lastValidIndex + 1, TickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        private static void TickInternal(int index)
        {
            GameClient client = _clients[index]; // Read lock unneeded.

            if (client?.EntityManagerId.IsSet != true)
                return;

            long startTick;
            long stopTick;

            try
            {
                switch (client.ClientState)
                {
                    case GameClient.eClientState.Disconnected:
                    case GameClient.eClientState.NotConnected:
                    case GameClient.eClientState.Linkdead:
                        return;
                    case GameClient.eClientState.CharScreen:
                    {
                        CheckCharScreenTimeout(client);
                        break;
                    }
                    case GameClient.eClientState.Playing:
                    {
                        CheckInGameTimeout(client);
                        GamePlayer player = client.Player;

                        if (player?.ObjectState == GameObject.eObjectState.Active)
                        {
                            if (ServiceUtils.ShouldTick(player.LastWorldUpdate + Properties.WORLD_PLAYER_UPDATE_INTERVAL))
                            {
                                startTick = GameLoop.GetCurrentTime();
                                UpdateWorld(player);
                                stopTick = GameLoop.GetCurrentTime();

                                if (stopTick - startTick > 25)
                                    log.Warn($"Long {SERVICE_NAME}.{nameof(UpdateWorld)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
                            }

                            player.movementComponent.Tick();
                        }

                        break;
                    }
                    default:
                    {
                        CheckHardTimeout(client);
                        break;
                    }
                }

                startTick = GameLoop.GetCurrentTime();
                client.PacketProcessor.ProcessTcpQueue();
                stopTick = GameLoop.GetCurrentTime();

                if (stopTick - startTick > 25)
                    log.Warn($"Long {SERVICE_NAME}.{nameof(client.PacketProcessor.ProcessTcpQueue)} for {client.Account.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, client.Player);
            }
        }

        public static void OnClientConnect(GameClient client)
        {
            if (EntityManager.Add(client))
                Interlocked.Increment(ref _clientCount);
            else if (log.IsWarnEnabled)
            {
                EntityManagerId entityManagerId = client.EntityManagerId;
                log.Warn($"{nameof(OnClientConnect)} was called but the client couldn't be added to the entity manager." +
                         $"(Client: {client})" +
                         $"(IsIdSet: {entityManagerId.IsSet})" +
                         $"(IsPendingAddition: {entityManagerId.IsPendingAddition})" +
                         $"(IsPendingRemoval: {entityManagerId.IsPendingAddition})" +
                         $"\n{Environment.StackTrace}");
            }
        }

        public static void OnClientDisconnect(GameClient client)
        {
            if (EntityManager.Remove(client))
                Interlocked.Decrement(ref _clientCount);
            else if (log.IsWarnEnabled)
            {
                EntityManagerId entityManagerId = client.EntityManagerId;
                log.Warn($"{nameof(OnClientDisconnect)} was called but the client couldn't be removed from the entity manager." +
                         $"(Client: {client})" +
                         $"(IsIdSet: {entityManagerId.IsSet})" +
                         $"(IsPendingAddition: {entityManagerId.IsPendingAddition})" +
                         $"(IsPendingRemoval: {entityManagerId.IsPendingAddition})" +
                         $"\n{Environment.StackTrace}");
            }
        }

        public static GamePlayer GetPlayer<T>(CheckPlayerAction<T> action)
        {
            return GetPlayer(action, default);
        }

        public static GamePlayer GetPlayer<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            using (_lock)
            {
                _lock.EnterReadLock();

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

            using (_lock)
            {
                _lock.EnterReadLock();

                foreach (GameClient client in _clients)
                {
                    if (client == null)
                        continue;

                    GamePlayer player = client.Player;

                    // Apparently 'Client.IsPlaying' can in sone cases be true even if it has no player. Need to figure out why.
                    if (player == null)
                        continue;

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
            using (_lock)
            {
                _lock.EnterReadLock();

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

            using (_lock)
            {
                _lock.EnterReadLock();

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
                if (!player.Client.IsPlaying || player.ObjectState is not GameObject.eObjectState.Active)
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
                if (!player.Client.IsPlaying || player.ObjectState is not GameObject.eObjectState.Active)
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
                using (_lock)
                {
                    _lock.EnterReadLock();
                    return _clients[id - 1];
                }
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
            int count = 0;

            using (_lock)
            {
                _lock.EnterReadLock();

                Parallel.ForEach(_clients, client =>
                {
                    if (client?.EntityManagerId.IsSet != true)
                        return;

                    client.SavePlayer();
                    Interlocked.Increment(ref count);
                });
            }

            return count;
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
            player.ItemUpdateCache[item] = (GameLoop.GameLoopTime, false);
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

        private static void CheckCharScreenTimeout(GameClient client)
        {
            if (ServiceUtils.ShouldTickNoEarly(client.PingTime + PING_TIMEOUT))
            {
                if (log.IsInfoEnabled)
                    log.Info($"Ping timeout on client. Disconnecting. ({client})");

                GameServer.Instance.Disconnect(client);
            }
        }
        private static void CheckHardTimeout(GameClient client)
        {
            if (ServiceUtils.ShouldTickNoEarly(client.PingTime + HARD_TIMEOUT))
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Hard timeout for client {client}");

                GameServer.Instance.Disconnect(client);
            }
        }

        private static void CheckInGameTimeout(GameClient client)
        {
            if (client.Player.IsLinkDeathTimerRunning)
                return;

            if (ServiceUtils.ShouldTickNoEarly(client.Player.LastPositionUpdateTime + POSITION_UPDATE_TIMEOUT))
            {
                if (log.IsInfoEnabled)
                    log.Info($"Position update timeout on client. Calling link death. ({client})");

                client.OnLinkDeath(true);
            }
            else if (Properties.KICK_IDLE_PLAYER_STATUS &&
                     ServiceUtils.ShouldTickNoEarly(client.Player.LastPlayerActivityTime + Properties.KICK_IDLE_PLAYER_TIME * 60000) &&
                     client.Account.PrivLevel == 1)
            {
                if (log.IsInfoEnabled)
                    log.Info($"Kicking inactive client to char screen. ({client})");

                ServiceUtils.KickPlayerToCharScreen(client.Player);
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

                if (!npc.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || npc.ObjectState is not GameObject.eObjectState.Active || !npc.IsVisibleTo(player))
                    npcUpdateCache.Remove(npc, out _);
            }

            List<GameNPC> npcsInRange = player.GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, WorldMgr.VISIBILITY_DISTANCE);
            GameObject targetObject = player.TargetObject;
            GameNPC pet = player.ControlledBrain?.Body;
            CachedNpcValues cachedTargetValues = null;
            CachedNpcValues cachedPetValues = null;

            foreach (GameNPC npcInRange in npcsInRange)
            {
                if (npcInRange.ObjectState is not GameObject.eObjectState.Active || !npcInRange.IsVisibleTo(player))
                    continue;

                if (!npcUpdateCache.TryGetValue(npcInRange, out CachedNpcValues cachedNpcValues))
                    UpdateObjectForPlayer(player, npcInRange);
                else if (ServiceUtils.ShouldTick(cachedNpcValues.Time + Properties.WORLD_NPC_UPDATE_INTERVAL))
                    UpdateObjectForPlayer(player, npcInRange);
                else if (ServiceUtils.ShouldTick(cachedNpcValues.Time + 250))
                {
                    // `GameNPC.HealthPercent` is a bit of an expensive call. Do it last.
                    if (npcInRange == targetObject)
                    {
                        if (npcInRange.HealthPercent > cachedNpcValues.HealthPercent)
                            cachedTargetValues = cachedNpcValues;
                    }
                    else if (npcInRange == pet)
                    {
                        if (npcInRange.HealthPercent > cachedNpcValues.HealthPercent)
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
            // The client is pretty stupid. It never forgets about static objects unless it moves too far away, but the distance seems to be anything between ~4500 and ~7500.
            // Not only that, but it forgets about objects even though it allows them to reappear after receiving a new packet while being at the same distance.
            // This means there's no way for us to know when the client actually needs a new packet.
            // We can send one at regular intervals, but this is wasteful, and the interval shouldn't be too long.
            // We can also assume the client doesn't need one if it's closer than ~4000 and has already received one.
            // The boolean keeps track of that. It becomes true (allowing further updates) if the client moves further than `STATIC_OBJECT_UPDATE_MIN_DISTANCE`, and becomes false on every update.
            // When true, updates are sent every `WORLD_OBJECT_UPDATE_INTERVAL`, as usual.
            // In short:
            // If the client forgets about the object at >`VISIBILITY_DISTANCE`, then it will reappear immediately when it gets back in range.
            // If the client forgets about the object at <`VISIBILITY_DISTANCE` but >`STATIC_OBJECT_UPDATE_MIN_DISTANCE`, then it will take up to `WORLD_OBJECT_UPDATE_INTERVAL` for it to reappear.
            // We assume the client cannot forget about the object when <`STATIC_OBJECT_UPDATE_MIN_DISTANCE`. If it does, the object won't reappear.

            ConcurrentDictionary<GameStaticItem, (long, bool)> itemUpdateCache = player.ItemUpdateCache;

            foreach (var itemInCache in itemUpdateCache)
            {
                GameStaticItem item = itemInCache.Key;

                if (!item.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || item.ObjectState is not GameObject.eObjectState.Active || !item.IsVisibleTo(player))
                    itemUpdateCache.Remove(item, out _);
                else if (!item.IsWithinRadius(player, STATIC_OBJECT_UPDATE_MIN_DISTANCE))
                    itemUpdateCache[item] = (itemUpdateCache[item].Item1, true);
            }

            List<GameStaticItem> itemsInRange = player.GetObjectsInRadius<GameStaticItem>(eGameObjectType.ITEM, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GameStaticItem itemInRange in itemsInRange)
            {
                if (itemInRange.ObjectState is not GameObject.eObjectState.Active || !itemInRange.IsVisibleTo(player))
                    continue;

                if (!itemUpdateCache.TryGetValue(itemInRange, out (long lastUpdate, bool allowFurtherUpdates) value) ||
                    (value.allowFurtherUpdates && ServiceUtils.ShouldTick(value.lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL)))
                {
                    CreateObjectForPlayer(player, itemInRange);
                }
            }
        }

        private static void UpdateDoors(GamePlayer player)
        {
            ConcurrentDictionary<GameDoorBase, long> doorUpdateCache = player.DoorUpdateCache;

            foreach (var doorInCache in doorUpdateCache)
            {
                GameDoorBase door = doorInCache.Key;

                if (!door.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || door.ObjectState is not GameObject.eObjectState.Active || !door.IsVisibleTo(player))
                    doorUpdateCache.Remove(door, out _);
            }

            List<GameDoorBase> doorsInRange = player.GetObjectsInRadius<GameDoorBase>(eGameObjectType.DOOR, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GameDoorBase doorInRange in doorsInRange)
            {
                if (doorInRange.ObjectState is not GameObject.eObjectState.Active || !doorInRange.IsVisibleTo(player))
                    continue;

                if (!doorUpdateCache.TryGetValue(doorInRange, out long lastUpdate))
                {
                    CreateObjectForPlayer(player, doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange);
                }
                else if (ServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
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
                else if (ServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
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
