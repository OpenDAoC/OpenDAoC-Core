using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using DOL.Database;
using DOL.GS.Housing;
using DOL.GS.ServerProperties;
using DOL.Logging;
using ECS.Debug;

namespace DOL.GS
{
    public sealed class ClientService : GameServiceBase
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        private const int HARD_TIMEOUT = 150000;
        private const int STATIC_OBJECT_UPDATE_MIN_DISTANCE = 4000;

        private List<GameClient> _clients = new();
        private SimpleDisposableLock _lock = new(LockRecursionPolicy.SupportsRecursion);
        private int _lastValidIndex;
        private int _clientCount;
        private GameClient[] _clientsBySessionId = new GameClient[ushort.MaxValue];
        private Trie<GamePlayer> _playerNameTrie = new();

        public int ClientCount => _clientCount;
        public static ClientService Instance { get; }

        static ClientService()
        {
            Instance = new();
        }

        public override void BeginTick()
        {
            ProcessPostedActionsParallel();

            using (_lock)
            {
                _lock.EnterWriteLock();

                try
                {
                    _clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(ServiceObjectType.Client, out _lastValidIndex);
                }
                catch (Exception e)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"{nameof(ServiceObjectStore.UpdateAndGetAll)} failed. Skipping this tick.", e);

                    _lastValidIndex = -1;
                    return;
                }
            }

            GameLoop.ExecuteForEach(_clients, _lastValidIndex + 1, BeginTickInternal);
        }

        public override void EndTick()
        {
            GameLoop.ExecuteForEach(_clients, _lastValidIndex + 1, EndTickInternal);

            if (Diagnostics.CheckServiceObjectCount)
                Diagnostics.PrintServiceObjectCount(ServiceName, ref EntityCount, _clients.Count);
        }

        private static void BeginTickInternal(GameClient client)
        {
            try
            {
                switch (client.ClientState)
                {
                    case GameClient.eClientState.NotConnected:
                    case GameClient.eClientState.Connecting:
                    case GameClient.eClientState.CharScreen:
                    case GameClient.eClientState.WorldEnter:
                    {
                        Receive(client);
                        CheckPingTimeout(client);
                        break;
                    }
                    case GameClient.eClientState.Playing:
                    {
                        Receive(client);
                        CheckPingTimeout(client);

                        GamePlayer player = client.Player;

                        if (player == null)
                            break;

                        CheckInGameActivityTimeout(client);

                        // The client state might have been modified by an inbound packet.
                        if (client.ClientState is not GameClient.eClientState.Playing || player.ObjectState is not GameObject.eObjectState.Active)
                            break;

                        // The rate at which clients send `UDPInitRequestHandler` may vary depending on their version (1.127 = 65 seconds).
                        if (GameServiceUtils.ShouldTick(client.UdpPingTime + 70000))
                            client.UdpConfirm = false;

                        if (GameServiceUtils.ShouldTick(player.NextWorldUpdate))
                        {
                            UpdateWorld(player);
                            player.NextWorldUpdate = GameLoop.GameLoopTime + Properties.WORLD_PLAYER_UPDATE_INTERVAL;
                        }

                        break;
                    }
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, client, client.Player);
            }
        }

        private static void EndTickInternal(GameClient client)
        {
            try
            {
                if (Diagnostics.CheckServiceObjectCount)
                    Interlocked.Increment(ref Instance.EntityCount);

                switch (client.ClientState)
                {
                    case GameClient.eClientState.Connecting:
                    case GameClient.eClientState.CharScreen:
                    case GameClient.eClientState.WorldEnter:
                    case GameClient.eClientState.Playing:
                    {
                        Send(client);
                        break;
                    }
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                GameServiceUtils.HandleServiceException(e, Instance.ServiceName, client, client.Player);
            }
        }

        private static void Receive(GameClient client)
        {
            long startTick = GameLoop.GetRealTime();
            client.Receive();
            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {Instance.ServiceName}.{nameof(Receive)} for {client.Account?.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
        }

        private static void Send(GameClient client)
        {
            long startTick = GameLoop.GetRealTime();
            client.PacketProcessor.SendPendingPackets();
            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {Instance.ServiceName}.{nameof(Send)} for {client.Account.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
        }

        public void OnClientConnect(GameClient client)
        {
            GameClient registeredClient = _clientsBySessionId[client.SessionId.Value];

            if (registeredClient != null && log.IsWarnEnabled)
            {
                log.Warn($"A client with the same session ID ({client.SessionId.Value}) was already registered." +
                    $"(Client: {client})" +
                    $"(Existing Client: {registeredClient})");
            }

            // Let's just overwrite the existing client. Most likely `OnClientDisconnect` was not called for some reason.
            _clientsBySessionId[client.SessionId.Value] = client;

            if (ServiceObjectStore.Add(client))
                Interlocked.Increment(ref _clientCount);
            else if (log.IsWarnEnabled)
            {
                ServiceObjectId serviceObjectId = client.ServiceObjectId;
                log.Warn($"{nameof(OnClientConnect)} was called but the client couldn't be added to the entity manager." +
                    $"(Client: {client})" +
                    $"(IsIdSet: {serviceObjectId.IsSet})" +
                    $"(IsPendingAddition: {serviceObjectId.IsPendingAddition})" +
                    $"(IsPendingRemoval: {serviceObjectId.IsPendingAddition})" +
                    $"\n{Environment.StackTrace}");
            }
        }

        public void OnClientDisconnect(GameClient client)
        {
            GameClient registeredClient = _clientsBySessionId[client.SessionId.Value];

            if (registeredClient == null && log.IsWarnEnabled)
                log.Warn($"A client with the session ID ({client.SessionId.Value}) was not registered. (Client: {client})");

            _clientsBySessionId[client.SessionId.Value] = null;

            if (ServiceObjectStore.Remove(client))
                Interlocked.Decrement(ref _clientCount);
            else if (log.IsWarnEnabled)
            {
                ServiceObjectId serviceObjectId = client.ServiceObjectId;
                log.Warn($"{nameof(OnClientDisconnect)} was called but the client couldn't be removed from the entity manager." +
                         $"(Client: {client})" +
                         $"(IsIdSet: {serviceObjectId.IsSet})" +
                         $"(IsPendingAddition: {serviceObjectId.IsPendingAddition})" +
                         $"(IsPendingRemoval: {serviceObjectId.IsPendingAddition})" +
                         $"\n{Environment.StackTrace}");
            }
        }

        public void OnPlayerJoin(GamePlayer player)
        {
            _playerNameTrie.Insert(player.Name, player);
        }

        public void OnPlayerLeave(GamePlayer player)
        {
            _playerNameTrie.Remove(player.Name, player);
        }

        public GamePlayer GetPlayer<T>(CheckPlayerAction<T> action)
        {
            return GetPlayer(action, default);
        }

        public GamePlayer GetPlayer<T>(CheckPlayerAction<T> action, T actionArgument)
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

        public List<GamePlayer> GetPlayers()
        {
            return GetPlayers<object>(null, null);
        }

        public List<GamePlayer> GetPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetPlayers(action, default);
        }

        public List<GamePlayer> GetPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            var players = GameLoop.GetListForTick<GamePlayer>();

            using (_lock)
            {
                _lock.EnterReadLock();

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

        public GameClient GetClient<T>(CheckClientAction<T> action)
        {
            return GetClient(action, default);
        }

        public GameClient GetClient<T>(CheckClientAction<T> action, T actionArgument)
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

        public List<GameClient> GetClients()
        {
            return GetClients<object>(null, null);
        }

        public List<GameClient> GetClients<T>(CheckClientAction<T> action)
        {
            return GetClients(action, default);
        }

        public List<GameClient> GetClients<T>(CheckClientAction<T> action, T actionArgument)
        {
            var clients = GameLoop.GetListForTick<GameClient>();

            using (_lock)
            {
                _lock.EnterReadLock();

                foreach (GameClient client in _clients)
                {
                    if (client?.Account == null)
                        continue;

                    if (action?.Invoke(client, actionArgument) != false)
                        clients.Add(client);
                }
            }

            return clients;
        }

        public GamePlayer GetPlayerByExactName(string playerName)
        {
            GamePlayer player = _playerNameTrie.FindExact(playerName);

            if (player == null)
                return null;

            if (player.ObjectState is GameObject.eObjectState.Deleted)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not active. Removing from trie. (Player: {player})");

                _playerNameTrie.Remove(playerName, player);
                return null;
            }

            return player;
        }

        public GamePlayer GetPlayerByPartialName(string playerName, out PlayerGuessResult result)
        {
            List<GamePlayer> matches = _playerNameTrie.FindByPrefix(playerName);

            if (matches.Count == 0)
            {
                result = PlayerGuessResult.NOT_FOUND;
                return null;
            }

            GamePlayer candidate = matches[0];

            // If the player is found but inactive, remove them from the trie and try the search again.
            // This handles the case for both exact and partial matches in one place.
            if (!ValidateAndRemoveIfInactive(candidate))
                return GetPlayerByPartialName(playerName, out result);

            // Exact match: the found player's name is the same length as the search query.
            if (candidate.Name.Length == playerName.Length)
            {
                result = PlayerGuessResult.FOUND_EXACT;
                return candidate;
            }

            // A single, partial match was found.
            if (matches.Count == 1)
            {
                result = PlayerGuessResult.FOUND_PARTIAL;
                return candidate;
            }

            // Multiple partial matches were found.
            result = PlayerGuessResult.FOUND_MULTIPLE;
            return null;

            bool ValidateAndRemoveIfInactive(GamePlayer player)
            {
                if (player.ObjectState is not GameObject.eObjectState.Deleted)
                    return true;

                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not active. Removing from trie. (Player: {player})");

                bool removed = _playerNameTrie.Remove(player.Name, player);

                if (!removed)
                {
                    if (log.IsErrorEnabled)
                        log.Error($"Failed to remove inactive player from trie: (Player: {player})");

                    // If removal failed, treat the player as valid to prevent infinite recursion.
                    return true;
                }

                return false;
            }
        }

        public List<GamePlayer> GetNonGmPlayers()
        {
            return GetNonGmPlayers<object>(null, default);
        }

        public List<GamePlayer> GetNonGmPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetNonGmPlayers(action, default);
        }

        public List<GamePlayer> GetNonGmPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            return GetPlayers(Predicate, (action, actionArgument));

            static bool Predicate(GamePlayer player, (CheckPlayerAction<T> action, T actionArgument) args)
            {
                return (ePrivLevel) player.Client.Account.PrivLevel == ePrivLevel.Player && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public List<GamePlayer> GetGmPlayers()
        {
            return GetGmPlayers<object>(null, default);
        }

        public List<GamePlayer> GetGmPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetGmPlayers(action, default);
        }

        public List<GamePlayer> GetGmPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            return GetPlayers(Predicate, (action, actionArgument));

            static bool Predicate(GamePlayer player, (CheckPlayerAction<T> action, T actionArgument) args)
            {
                return (ePrivLevel) player.Client.Account.PrivLevel > ePrivLevel.Player && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public List<GamePlayer> GetPlayersOfRealm(eRealm realm)
        {
            return GetPlayersOfRealm<object>((realm, default, default));
        }

        public List<GamePlayer> GetPlayersOfRealm<T>(eRealm realm, CheckPlayerAction<T> action)
        {
            return GetPlayersOfRealm((realm, action, default));
        }

        public List<GamePlayer> GetPlayersOfRealm<T>((eRealm, CheckPlayerAction<T>, T) args)
        {
            return GetPlayers(Predicate, args);

            static bool Predicate(GamePlayer player, (eRealm realm, CheckPlayerAction<T> action, T actionArgument) args)
            {
                return player.Realm == args.realm && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public List<GamePlayer> GetPlayersOfRegion(Region region)
        {
            return GetPlayersOfRegion<object>((region, default, default));
        }

        public List<GamePlayer> GetPlayersOfRegion<T>(Region region, CheckPlayerAction<T> action)
        {
            return GetPlayersOfRegion((region, action, default));
        }

        public List<GamePlayer> GetPlayersOfRegion<T>((Region, CheckPlayerAction<T>, T) args)
        {
            return GetPlayers(Predicate, args);

            static bool Predicate(GamePlayer player, (Region region, CheckPlayerAction<T> action, T actionArgument) args)
            {
                return player.CurrentRegion == args.region && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public List<GamePlayer> GetPlayersOfRegionAndRealm(Region region, eRealm realm)
        {
            return GetPlayersOfRegion((region, Predicate, realm));

            static bool Predicate(GamePlayer player, eRealm realm)
            {
                return player.Realm == realm;
            }
        }

        public List<GamePlayer> GetPlayersOfZone(Zone zone)
        {
            return GetPlayers(Predicate, zone);

            static bool Predicate(GamePlayer player, Zone zone)
            {
                return player.CurrentZone == zone;
            }
        }

        // Advice, Broadcast, LFG, Trade.
        public List<GamePlayer> GetPlayersForRealmWideChatMessage(GamePlayer sender)
        {
            return GetPlayers(Predicate, sender);

            static bool Predicate(GamePlayer player, GamePlayer sender)
            {
                return GameServer.ServerRules.IsAllowedToUnderstand(sender, player) && !player.IsIgnoring(sender);
            }
        }

        public GameClient GetClientBySessionId(int id)
        {
            return id < 1 || id >= _clientsBySessionId.Length ? null : _clientsBySessionId[id];
        }

        public GameClient GetClientFromAccount(DbAccount account)
        {
            return GetClient(Predicate, account);

            static bool Predicate(GameClient client, DbAccount account)
            {
                return client.Account != null && client.Account == account;
            }
        }

        public GameClient GetClientFromAccountName(string accountName)
        {
            return GetClient(Predicate, accountName);

            static bool Predicate(GameClient client, string accountName)
            {
                return client.Account != null && client.Account.Name.Equals(accountName);
            }
        }

        public GameClient GetClientWithSameIp(GameClient otherClient)
        {
            return GetClient(Predicate, otherClient);

            static bool Predicate(GameClient client, GameClient otherClient)
            {
                return client.Account != null && (ePrivLevel) client.Account.PrivLevel <= ePrivLevel.Player && client.TcpEndpointAddress.Equals(otherClient.TcpEndpointAddress) && client != otherClient;
            }
        }

        public int SavePlayers()
        {
            List<GamePlayer> players = GetPlayers();

            foreach (GamePlayer player in players)
                player.SaveIntoDatabase();

            return players.Count;
        }

        private static void AddNpcToPlayerCache(GamePlayer player, GameNPC npc)
        {
            lock (player.PlayerObjectCache.NpcUpdateCacheLock)
            {
                if (player.PlayerObjectCache.NpcUpdateCache.TryGetValue(npc, out CachedNpcValues cachedNpcValues))
                {
                    cachedNpcValues.LastUpdateTime = GameLoop.GameLoopTime;
                    cachedNpcValues.HealthPercent =  npc.HealthPercent;
                }
                else
                    player.PlayerObjectCache.NpcUpdateCache[npc] = new(GameLoop.GameLoopTime, npc.HealthPercent);
            }
        }

        private static void AddItemToPlayerCache(GamePlayer player, GameStaticItem item)
        {
            lock (player.PlayerObjectCache.ItemUpdateCacheLock)
            {
                player.PlayerObjectCache.ItemUpdateCache[item] = new(GameLoop.GameLoopTime, false);
            }
        }

        private static void AddDoorToPlayerCache(GamePlayer player, GameDoorBase door)
        {
            lock (player.PlayerObjectCache.DoorUpdateCacheLock)
            {
                player.PlayerObjectCache.DoorUpdateCache[door] = GameLoop.GameLoopTime;
            }
        }

        private static void AddHouseToPlayerCache(GamePlayer player, House house)
        {
            lock (player.PlayerObjectCache.HouseUpdateCacheLock)
            {
                player.PlayerObjectCache.HouseUpdateCache[house] = GameLoop.GameLoopTime;
            }
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

        private static void OnObjectCreateOrUpdateForPlayer(GamePlayer player, GameObject gameObject)
        {
            gameObject.OnUpdateOrCreateForPlayer();
            AddObjectToPlayerCache(player, gameObject);
        }

        private static void UpdateObjectForPlayerInternal(GamePlayer player, GameObject gameObject, bool udp = true)
        {
            player.Out.SendObjectUpdate(gameObject, udp);
            OnObjectCreateOrUpdateForPlayer(player, gameObject);
        }

        public static void UpdateObjectForPlayer(GamePlayer player, GameObject gameObject)
        {
            if (player.Client.ClientState is not GameClient.eClientState.Playing)
                return;

            UpdateObjectForPlayerInternal(player, gameObject);
        }

        public static void UpdateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                UpdateObjectForPlayer(player, gameObject);
        }

        public static void UpdateNpcForPlayer(GamePlayer player, GameNPC npc)
        {
            if (player.Client.ClientState is not GameClient.eClientState.Playing || !player.CanDetect(npc))
                return;

            UpdateObjectForPlayerInternal(player, npc);
        }

        public static void UpdateNpcForPlayers(GameNPC npc)
        {
            foreach (GamePlayer player in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                UpdateNpcForPlayer(player, npc);
        }

        private static void CreateNpcForPlayerInternal(GamePlayer player, GameNPC npc)
        {
            player.Out.SendNPCCreate(npc);

            if (npc.Inventory != null)
                player.Out.SendLivingEquipmentUpdate(npc);

            // Dirty fix preventing the client from losing its target when its a NPC.
            if (player.TargetObject == npc)
                player.Out.SendChangeTarget(npc);

            OnObjectCreateOrUpdateForPlayer(player, npc);
        }

        private static void CreateObjectForPlayerInternal(GamePlayer player, GameObject gameObject)
        {
            player.Out.SendObjectCreate(gameObject);
            AddObjectToPlayerCache(player, gameObject);
        }

        public static void CreateObjectForPlayer(GamePlayer player, GameObject gameObject)
        {
            if (player.Client.ClientState is not GameClient.eClientState.Playing)
                return;

            if (gameObject.GameObjectType is eGameObjectType.NPC)
            {
                if (!player.CanDetect(gameObject))
                    return;

                CreateNpcForPlayerInternal(player, gameObject as GameNPC);
            }
            else
                CreateObjectForPlayerInternal(player, gameObject);
        }

        public static void CreateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                CreateObjectForPlayer(player, gameObject);
        }

        private static void CheckPingTimeout(GameClient client)
        {
            if (GameServiceUtils.ShouldTick(client.PingTime + HARD_TIMEOUT))
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Hard timeout on client. Disconnecting. ({client})");

                client.Disconnect();
            }
        }

        private static void CheckInGameActivityTimeout(GameClient client)
        {
            if (Properties.KICK_IDLE_PLAYER_STATUS &&
                GameServiceUtils.ShouldTick(client.Player.LastPlayerActivityTime + Properties.KICK_IDLE_PLAYER_TIME * 60000) &&
                client.Account.PrivLevel == 1)
            {
                if (log.IsInfoEnabled)
                    log.Info($"Kicking inactive client to char screen. ({client})");

                GameServiceUtils.KickPlayerToCharScreen(client.Player);
            }
        }

        private static void UpdateWorld(GamePlayer player)
        {
            // Players aren't updated here on purpose.
            long startTick = GameLoop.GetRealTime();

            lock (player.PlayerObjectCache.NpcUpdateCacheLock)
            {
                UpdateNpcs(player);
            }

            lock (player.PlayerObjectCache.ItemUpdateCacheLock)
            {
                UpdateItems(player);
            }

            lock (player.PlayerObjectCache.DoorUpdateCacheLock)
            {
                UpdateDoors(player);
            }

            lock (player.PlayerObjectCache.HouseUpdateCacheLock)
            {
                UpdateHouses(player);
            }

            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {Instance.ServiceName}.{nameof(UpdateWorld)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
        }

        private static void UpdateNpcs(GamePlayer player)
        {
            HashSet<GameNPC> inRangeSet = player.PlayerObjectCache.NpcInRangeCache;
            Dictionary<GameNPC, CachedNpcValues> npcUpdateCache = player.PlayerObjectCache.NpcUpdateCache;

            foreach (GameNPC npc in player.GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, WorldMgr.VISIBILITY_DISTANCE))
            {
                if (npc.ObjectState is GameObject.eObjectState.Active && npc.IsVisibleTo(player) && player.CanDetect(npc))
                    inRangeSet.Add(npc);
            }

            foreach (var pair in npcUpdateCache)
            {
                GameNPC cachedNpc = pair.Key;

                if (!inRangeSet.Contains(cachedNpc))
                    npcUpdateCache.Remove(cachedNpc);
            }

            GameObject targetObject = player.TargetObject;
            GameNPC pet = player.ControlledBrain?.Body;

            foreach (GameNPC npcInRange in inRangeSet)
            {
                if (!npcUpdateCache.TryGetValue(npcInRange, out CachedNpcValues cachedNpcValues))
                    CreateNpcForPlayerInternal(player, npcInRange);
                else if (GameServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + Properties.WORLD_NPC_UPDATE_INTERVAL))
                    UpdateObjectForPlayerInternal(player, npcInRange, false);
                else if (npcInRange == targetObject || npcInRange == pet)
                {
                    if (GameServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + 250) && npcInRange.HealthPercent != cachedNpcValues.HealthPercent)
                        UpdateObjectForPlayerInternal(player, npcInRange);
                }
            }

            inRangeSet.Clear();
        }

        private static void UpdateItems(GamePlayer player)
        {
            // The client is pretty stupid. It never forgets about static objects unless it moves too far away, but the distance seems to be anything between ~4500 and ~7500.
            // Not only that, but it forgets about objects even though it allows them to reappear after receiving a new packet while being at the same distance.
            // This means there's no way for us to know when the client actually needs a new packet.

            HashSet<GameStaticItem> inRangeSet = player.PlayerObjectCache.ItemInRangeCache;
            Dictionary<GameStaticItem, CachedItemValues> itemUpdateCache = player.PlayerObjectCache.ItemUpdateCache;

            foreach (GameStaticItem item in player.GetObjectsInRadius<GameStaticItem>(eGameObjectType.ITEM, WorldMgr.VISIBILITY_DISTANCE))
            {
                if (item.ObjectState is GameObject.eObjectState.Active && item.IsVisibleTo(player))
                    inRangeSet.Add(item);
            }

            foreach (var item in itemUpdateCache)
            {
                GameStaticItem cachedItem = item.Key;

                if (!inRangeSet.Contains(cachedItem))
                    itemUpdateCache.Remove(cachedItem);
            }

            foreach (GameStaticItem itemInRange in inRangeSet)
            {
                if (!itemUpdateCache.TryGetValue(itemInRange, out CachedItemValues cachedItemValues) ||
                    GameServiceUtils.ShouldTick(cachedItemValues.LastUpdateTime + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
                {
                    // There'is no update packet for items.
                    CreateObjectForPlayerInternal(player, itemInRange);
                }
            }

            inRangeSet.Clear();
        }

        private static void UpdateDoors(GamePlayer player)
        {
            HashSet<GameDoorBase> inRangeSet = player.PlayerObjectCache.DoorInRangeCache;
            Dictionary<GameDoorBase, long> doorUpdateCache = player.PlayerObjectCache.DoorUpdateCache;

            foreach (GameDoorBase door in player.GetObjectsInRadius<GameDoorBase>(eGameObjectType.DOOR, WorldMgr.VISIBILITY_DISTANCE))
            {
                if (door.ObjectState is GameObject.eObjectState.Active && door.IsVisibleTo(player))
                    inRangeSet.Add(door);
            }

            foreach (var door in doorUpdateCache)
            {
                GameDoorBase doorInCache = door.Key;

                if (!inRangeSet.Contains(doorInCache))
                    doorUpdateCache.Remove(doorInCache);
            }

            foreach (GameDoorBase doorInRange in inRangeSet)
            {
                if (!doorUpdateCache.TryGetValue(doorInRange, out long lastUpdate))
                {
                    CreateObjectForPlayerInternal(player, doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange); // Not handled by `CreateObjectForPlayer`.
                }
                else if (GameServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
                    UpdateObjectForPlayerInternal(player, doorInRange, false);
            }

            inRangeSet.Clear();
        }

        private static void UpdateHouses(GamePlayer player)
        {
            if (!player.CurrentRegion.HousingEnabled)
                return;

            HashSet<House> inRangeSet = player.PlayerObjectCache.HouseInRangeCache;
            Dictionary<House, long> houseUpdateCache = player.PlayerObjectCache.HouseUpdateCache;

            foreach (House house in HouseMgr.GetHouses(player.CurrentRegionID).Values)
            {
                if (house.RegionID == player.CurrentRegionID && house.IsWithinRadius(player, HousingConstants.HouseViewingDistance))
                    inRangeSet.Add(house);
            }

            foreach (var house in houseUpdateCache)
            {
                House houseInCache = house.Key;

                if (!inRangeSet.Contains(houseInCache))
                    houseUpdateCache.Remove(houseInCache);
            }

            foreach (House house in inRangeSet)
            {
                if (!player.PlayerObjectCache.HouseUpdateCache.TryGetValue(house, out long lastUpdate))
                {
                    player.Client.Out.SendHouse(house);
                    player.Client.Out.SendGarden(house);
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
                }
                else if (GameServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);

                AddHouseToPlayerCache(player, house);
            }

            inRangeSet.Clear();
        }

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
            public long LastUpdateTime { get; set; }
            public byte HealthPercent { get; set; }

            public CachedNpcValues(long time, byte healthPercent)
            {
                LastUpdateTime = time;
                HealthPercent = healthPercent;
            }
        }

        public class CachedItemValues
        {
            public long LastUpdateTime { get; set; }

            public CachedItemValues(long lastUpdate, bool allowFurtherUpdate)
            {
                LastUpdateTime = lastUpdate;
            }
        }
    }
}
