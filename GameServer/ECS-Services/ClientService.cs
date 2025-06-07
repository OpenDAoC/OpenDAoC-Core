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

namespace DOL.GS
{
    public static class ClientService
    {
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(ClientService);
        private const string SERVICE_NAME_BEGIN = $"{SERVICE_NAME}_Begin";
        private const string SERVICE_NAME_END = $"{SERVICE_NAME}_End";
        private const int HARD_TIMEOUT = 150000;
        private const int STATIC_OBJECT_UPDATE_MIN_DISTANCE = 4000;

        private static List<GameClient> _clients = new();
        private static int _entityCount; // For diagnostics.
        private static SimpleDisposableLock _lock = new(LockRecursionPolicy.SupportsRecursion);
        private static int _lastValidIndex;
        private static int _clientCount;
        private static GameClient[] _clientsBySessionId = new GameClient[ushort.MaxValue]; // Fast lookup by session ID.
        private static Trie<GamePlayer> _playerNameTrie = new();

        public static int ClientCount => _clientCount; // `_clients` contains null objects.

        public static void BeginTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_BEGIN;
            Diagnostics.StartPerfCounter(SERVICE_NAME_BEGIN);

            using (_lock)
            {
                _lock.EnterWriteLock();
                _clients = ServiceObjectStore.UpdateAndGetAll<GameClient>(ServiceObjectType.Client, out _lastValidIndex);
            }

            GameLoop.ExecuteWork(_lastValidIndex + 1, BeginTickInternal);
            Diagnostics.StopPerfCounter(SERVICE_NAME_BEGIN);
        }

        public static void EndTick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME_END;
            Diagnostics.StartPerfCounter(SERVICE_NAME_END);
            GameLoop.ExecuteWork(_lastValidIndex + 1, EndTickInternal);

            if (Diagnostics.CheckEntityCounts)
                Diagnostics.PrintEntityCount(SERVICE_NAME, ref _entityCount, _clients.Count);

            Diagnostics.StopPerfCounter(SERVICE_NAME_END);
        }

        private static void BeginTickInternal(int index)
        {
            GameClient client = null;

            try
            {
                client = _clients[index];

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

                        if (ServiceUtils.ShouldTick(player.LastWorldUpdate + Properties.WORLD_PLAYER_UPDATE_INTERVAL))
                        {
                            UpdateWorld(player);
                            player.LastWorldUpdate = GameLoop.GameLoopTime + Properties.WORLD_PLAYER_UPDATE_INTERVAL;
                        }

                        break;
                    }
                    default:
                        return;
                }
            }
            catch (Exception e)
            {
                ServiceUtils.HandleServiceException(e, SERVICE_NAME_BEGIN, client, client.Player);
            }
        }

        private static void EndTickInternal(int index)
        {
            GameClient client = null;

            try
            {
                if (Diagnostics.CheckEntityCounts)
                    Interlocked.Increment(ref _entityCount);

                client = _clients[index];

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
                ServiceUtils.HandleServiceException(e, SERVICE_NAME_END, client, client.Player);
            }
        }

        private static void Receive(GameClient client)
        {
            long startTick = GameLoop.GetRealTime();
            client.Receive();
            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {SERVICE_NAME_BEGIN}.{nameof(Receive)} for {client.Account?.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
        }

        private static void Send(GameClient client)
        {
            long startTick = GameLoop.GetRealTime();
            client.PacketProcessor.SendPendingPackets();
            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {SERVICE_NAME_END}.{nameof(Send)} for {client.Account.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
        }

        public static void OnClientConnect(GameClient client)
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

        public static void OnClientDisconnect(GameClient client)
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

        public static void OnPlayerJoin(GamePlayer player)
        {
            _playerNameTrie.Insert(player.Name, player);
        }

        public static void OnPlayerLeave(GamePlayer player)
        {
            _playerNameTrie.Remove(player.Name, player);
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

        public static GamePlayer GetPlayerByExactName(string playerName)
        {
            GamePlayer player = _playerNameTrie.FindExact(playerName);

            if (!player.Client.IsPlaying || player.ObjectState is not GameObject.eObjectState.Active)
            {
                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not playing or is not active. Removing from trie. (Player: {player})");

                _playerNameTrie.Remove(playerName, player);
                return null;
            }

            return player;
        }

        public static GamePlayer GetPlayerByPartialName(string playerName, out PlayerGuessResult result)
        {
            List<GamePlayer> matches = _playerNameTrie.FindByPrefix(playerName);

            if (matches.Count == 0)
            {
                result = PlayerGuessResult.NOT_FOUND;
                return null;
            }

            GamePlayer player = matches[0];

            // The first element may be an exact match.
            if (player.Name.Length == playerName.Length)
            {
                if (ValidateAndRemoveIfInactive(player))
                    return GetPlayerByPartialName(playerName, out result);

                result = PlayerGuessResult.FOUND_EXACT;
                return player;
            }

            // Partial match found.
            if (matches.Count == 1)
            {
                if (ValidateAndRemoveIfInactive(player))
                    return GetPlayerByPartialName(playerName, out result);

                result = PlayerGuessResult.FOUND_PARTIAL;
                return player;
            }

            // Multiple matches found.
            result = PlayerGuessResult.FOUND_MULTIPLE;
            return null;

            bool ValidateAndRemoveIfInactive(GamePlayer player)
            {
                if (player.Client.IsPlaying && player.ObjectState is GameObject.eObjectState.Active)
                    return false;

                if (log.IsErrorEnabled)
                    log.Error($"Player was found in the trie, but is not playing or is not active. Removing from trie. (Player: {player})");

                _playerNameTrie.Remove(playerName, player);
                return true;
            }
        }

        public static List<GamePlayer> GetNonGmPlayers()
        {
            return GetNonGmPlayers<object>(null, default);
        }

        public static List<GamePlayer> GetNonGmPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetNonGmPlayers(action, default);
        }

        public static List<GamePlayer> GetNonGmPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            return GetPlayers(Predicate, (action, actionArgument));

            static bool Predicate(GamePlayer player, (CheckPlayerAction<T> action, T actionArgument) args)
            {
                return (ePrivLevel) player.Client.Account.PrivLevel == ePrivLevel.Player && args.action?.Invoke(player, args.actionArgument) != false;
            }
        }

        public static List<GamePlayer> GetGmPlayers()
        {
            return GetGmPlayers<object>(null, default);
        }

        public static List<GamePlayer> GetGmPlayers<T>(CheckPlayerAction<T> action)
        {
            return GetGmPlayers(action, default);
        }

        public static List<GamePlayer> GetGmPlayers<T>(CheckPlayerAction<T> action, T actionArgument)
        {
            return GetPlayers(Predicate, (action, actionArgument));

            static bool Predicate(GamePlayer player, (CheckPlayerAction<T> action, T actionArgument) args)
            {
                return (ePrivLevel) player.Client.Account.PrivLevel > ePrivLevel.Player && args.action?.Invoke(player, args.actionArgument) != false;
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

        public static GameClient GetClientBySessionId(int id)
        {
            return id < 1 || id >= _clientsBySessionId.Length ? null : _clientsBySessionId[id];
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
                return client.Account != null && (ePrivLevel) client.Account.PrivLevel <= ePrivLevel.Player && client.TcpEndpointAddress.Equals(otherClient.TcpEndpointAddress) && client != otherClient;
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
                    if (client?.ServiceObjectId.IsSet != true)
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
                cachedNpcValues.LastUpdateTime = GameLoop.GameLoopTime;
                cachedNpcValues.HealthPercent =  npc.HealthPercent;
            }
            else
                player.NpcUpdateCache[npc] = new(GameLoop.GameLoopTime, npc.HealthPercent);
        }

        private static void AddItemToPlayerCache(GamePlayer player, GameStaticItem item)
        {
            player.ItemUpdateCache[item] = new(GameLoop.GameLoopTime, false);
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

        public static void CreateNpcForPlayer(GamePlayer player, GameNPC npc)
        {
            if (player.Client.ClientState is not GameClient.eClientState.Playing || !player.CanDetect(npc))
                return;

            CreateNpcForPlayerInternal(player, npc);
        }

        public static void CreateNpcForPlayers(GameNPC npc)
        {
            foreach (GamePlayer playerInRadius in npc.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                CreateNpcForPlayer(playerInRadius, npc);
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

            CreateObjectForPlayerInternal(player, gameObject);
        }

        public static void CreateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                CreateObjectForPlayer(player, gameObject);
        }

        private static void CheckPingTimeout(GameClient client)
        {
            if (ServiceUtils.ShouldTick(client.PingTime + HARD_TIMEOUT))
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Hard timeout on client. Disconnecting. ({client})");

                client.Disconnect();
            }
        }

        private static void CheckInGameActivityTimeout(GameClient client)
        {
            if (Properties.KICK_IDLE_PLAYER_STATUS &&
                ServiceUtils.ShouldTick(client.Player.LastPlayerActivityTime + Properties.KICK_IDLE_PLAYER_TIME * 60000) &&
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
            long startTick = GameLoop.GetRealTime();
            UpdateNpcs(player);
            UpdateItems(player);
            UpdateDoors(player);
            UpdateHouses(player);
            long stopTick = GameLoop.GetRealTime();

            if (stopTick - startTick > Diagnostics.LongTickThreshold)
                log.Warn($"Long {SERVICE_NAME_BEGIN}.{nameof(UpdateWorld)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
        }

        private static void UpdateNpcs(GamePlayer player)
        {
            ConcurrentDictionary<GameNPC, CachedNpcValues> npcUpdateCache = player.NpcUpdateCache;

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
                else if (ServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + Properties.WORLD_NPC_UPDATE_INTERVAL))
                    UpdateObjectForPlayerInternal(player, npcInRange, false);
                else if (ServiceUtils.ShouldTick(cachedNpcValues.LastUpdateTime + 250))
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

            ConcurrentDictionary<GameStaticItem, CachedItemValues> itemUpdateCache = player.ItemUpdateCache;

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
                    (cachedItemValues.AllowFurtherUpdate && ServiceUtils.ShouldTick(cachedItemValues.LastUpdateTime + Properties.WORLD_OBJECT_UPDATE_INTERVAL)))
                {
                    CreateObjectForPlayerInternal(player, itemInRange);
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
                    CreateObjectForPlayerInternal(player, doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange); // Not handled by `CreateObjectForPlayer`.
                }
                else if (ServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
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
                else if (ServiceUtils.ShouldTick(lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL))
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
