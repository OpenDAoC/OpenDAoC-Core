using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
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

        public static void Tick()
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<GameClient> list = EntityManager.UpdateAndGetAll<GameClient>(EntityManager.EntityType.Client, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                GameClient client = list[i];

                if (client?.EntityManagerId.IsSet != true)
                    return;

                GamePlayer player = client.Player;

                if (player != null &&
                    player.Client.ClientState == GameClient.eClientState.Playing &&
                    player.ObjectState == GameObject.eObjectState.Active)
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

                if (client.IsConnected)
                {
                    try
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        client.PacketProcessor?.ProcessTcpQueue();
                        long stopTick = GameLoop.GetCurrentTime();

                        if (stopTick - startTick > 25)
                            log.Warn($"Long {SERVICE_NAME}.{nameof(client.PacketProcessor.ProcessTcpQueue)} for {client.Account?.Name}({client.SessionID}) Time: {stopTick - startTick}ms");
                    }
                    catch (Exception e)
                    {
                        ServiceUtils.HandleServiceException(e, SERVICE_NAME, client, player);
                    }
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
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
