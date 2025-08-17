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
        public static new ClientService Instance { get; }

        static ClientService()
        {
            Instance = new();
        }

        public override void BeginTick()
        {
            ProcessPostedActions();

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
            List<GamePlayer> players = new();

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
            List<GameClient> clients = new();

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

            if (!player.Client.IsPlaying || player.ObjectState is not GameObject.eObjectState.Active)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not playing or is not active. Removing from trie. (Player: {player})");

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
                if (player.Client.IsPlaying && player.ObjectState is GameObject.eObjectState.Active)
                    return true;

                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not playing or is not active. Removing from trie. (Player: {player})");

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
            lock (player.NpcUpdateCacheLock)
            {
                if (player.NpcUpdateCache.TryGetValue(npc, out CachedNpcValues cachedNpcValues))
                {
                    cachedNpcValues.LastUpdateTime = GameLoop.GameLoopTime;
                    cachedNpcValues.HealthPercent =  npc.HealthPercent;
                }
                else
                    player.NpcUpdateCache[npc] = new(GameLoop.GameLoopTime, npc.HealthPercent);
            }
        }

        private static void AddItemToPlayerCache(GamePlayer player, GameStaticItem item)
        {
            lock (player.ItemUpdateCacheLock)
            {
                player.ItemUpdateCache[item] = new(GameLoop.GameLoopTime, false);
            }
        }

        private static void AddDoorToPlayerCache(GamePlayer player, GameDoorBase door)
        {
            lock (player.DoorUpdateCacheLock)
            {
                player.DoorUpdateCache[door] = GameLoop.GameLoopTime;
            }
        }

        private static void AddHouseToPlayerCache(GamePlayer player, House house)
        {
            lock (player.HouseUpdateCacheLock)
            {
                player.HouseUpdateCache[house] = GameLoop.GameLoopTime;
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

            lock (player.NpcUpdateCacheLock)
            {
                UpdateNpcs(player);
            }

            lock (player.ItemUpdateCacheLock)
            {
                UpdateItems(player);
            }

            lock (player.DoorUpdateCacheLock)
            {
                UpdateDoors(player);
            }

            lock (player.HouseUpdateCacheLock)
            {
                UpdateHouses(player);
            }

            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {ClientService.Instance.ServiceName}.{nameof(UpdateWorld)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
        }

        private static void UpdateNpcs(GamePlayer player)
        {
            Dictionary<GameNPC, CachedNpcValues> npcUpdateCache = player.NpcUpdateCache;

            foreach (var npcInCache in npcUpdateCache)
            {
                GameNPC npc = npcInCache.Key;

                if (!npc.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || npc.ObjectState is not GameObject.eObjectState.Active || !npc.IsVisibleTo(player))
                    npcUpdateCache.Remove(npc, out _);
                else if (!player.CanDetect(npc))
                {
                    // Prevents NPCs from staying visible for a few seconds after getting out of range.
                    // Not really needed in other cases.
                    player.Out.SendObjectRemove(npc);
                    npcUpdateCache.Remove(npc, out _);
                }
            }

            List<GameNPC> npcsInRange = player.GetObjectsInRadius<GameNPC>(eGameObjectType.NPC, WorldMgr.VISIBILITY_DISTANCE);
            GameObject targetObject = player.TargetObject;
            GameNPC pet = player.ControlledBrain?.Body;
            CachedNpcValues cachedTargetValues = null;
            CachedNpcValues cachedPetValues = null;

            foreach (GameNPC npcInRange in npcsInRange)
            {
                if (npcInRange.ObjectState is not GameObject.eObjectState.Active || !npcInRange.IsVisibleTo(player) || !player.CanDetect(npcInRange))
                    continue;

                if (!npcUpdateCache.TryGetValue(npcInRange, out CachedNpcValues cachedNpcValues))
                    CreateNpcForPlayerInternal(player, npcInRange);
                else if (GameServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + Properties.WORLD_NPC_UPDATE_INTERVAL))
                    UpdateObjectForPlayerInternal(player, npcInRange, false);
                else if (GameServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + 250))
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
                UpdateObjectForPlayerInternal(player, targetObject);

            if (cachedPetValues != null)
                UpdateObjectForPlayerInternal(player, pet);
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

            Dictionary<GameStaticItem, CachedItemValues> itemUpdateCache = player.ItemUpdateCache;

            foreach (var itemInCache in itemUpdateCache)
            {
                GameStaticItem item = itemInCache.Key;

                if (!item.IsWithinRadius(player, WorldMgr.VISIBILITY_DISTANCE) || item.ObjectState is not GameObject.eObjectState.Active || !item.IsVisibleTo(player))
                    itemUpdateCache.Remove(item, out _);
                else if (!item.IsWithinRadius(player, STATIC_OBJECT_UPDATE_MIN_DISTANCE))
                    itemUpdateCache[item].AllowFurtherUpdate = true;
            }

            List<GameStaticItem> itemsInRange = player.GetObjectsInRadius<GameStaticItem>(eGameObjectType.ITEM, WorldMgr.VISIBILITY_DISTANCE);

            foreach (GameStaticItem itemInRange in itemsInRange)
            {
                if (itemInRange.ObjectState is not GameObject.eObjectState.Active || !itemInRange.IsVisibleTo(player))
                    continue;

                if (!itemUpdateCache.TryGetValue(itemInRange, out CachedItemValues cachedItemValues) ||
                    (cachedItemValues.AllowFurtherUpdate && GameServiceUtils.ShouldTick(cachedItemValues.LastUpdateTime + Properties.WORLD_OBJECT_UPDATE_INTERVAL)))
                {
                    CreateObjectForPlayerInternal(player, itemInRange);
                }
            }
        }

        private static void UpdateDoors(GamePlayer player)
        {
            Dictionary<GameDoorBase, long> doorUpdateCache = player.DoorUpdateCache;

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
                    CreateObjectForPlayerInternal(player, doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange); // Not handled by `CreateObjectForPlayer`.
                }
                else if (GameServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
                    UpdateObjectForPlayerInternal(player, doorInRange, false);
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
                else if (GameServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);

                AddHouseToPlayerCache(player, house);
            }
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
            public bool AllowFurtherUpdate { get; set; }

            public CachedItemValues(long lastUpdate, bool allowFurtherUpdate)
            {
                LastUpdateTime = lastUpdate;
                AllowFurtherUpdate = allowFurtherUpdate;
            }
        }
    }
}
