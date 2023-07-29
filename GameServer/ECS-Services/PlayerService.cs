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
    public static class PlayerService
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private const string SERVICE_NAME = nameof(PlayerService);

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<GamePlayer> list = EntityManager.UpdateAndGetAll<GamePlayer>(EntityManager.EntityType.Player, out int lastValidIndex);

            Parallel.For(0, lastValidIndex + 1, i =>
            {
                GamePlayer player = list[i];

                if (player?.EntityManagerId.IsSet != true ||
                    player.Client.ClientState != GameClient.eClientState.Playing ||
                    player.ObjectState != GameObject.eObjectState.Active)
                {
                    return;
                }

                try
                {
                    if (player.LastWorldUpdate + Properties.WORLD_PLAYER_UPDATE_INTERVAL < tick)
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        UpdateWorld(player, tick);
                        long stopTick = GameLoop.GetCurrentTime();

                        if (stopTick - startTick > 25)
                            log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
                    }

                    player.movementComponent.Tick(tick);
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, player, player);
                }
            });

            Diagnostics.StopPerfCounter(SERVICE_NAME);
        }

        public static void UpdateObjectForPlayer(GamePlayer player, GameObject gameObject)
        {
            gameObject.OnUpdateByPlayerService();
            player.Out.SendObjectUpdate(gameObject);
            player.ObjectUpdateCaches[(byte) gameObject.GameObjectType][gameObject] = GameLoop.GameLoopTime;
        }

        public static void UpdateObjectForPlayers(GameObject gameObject)
        {
            foreach (GamePlayer player in gameObject.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                UpdateObjectForPlayer(player, gameObject);
        }

        private static void UpdateWorld(GamePlayer player, long tick)
        {
            // Players aren't updated here on purpose.
            UpdateObjects(player, eGameObjectType.NPC, Properties.WORLD_NPC_UPDATE_INTERVAL, tick);
            UpdateObjects(player, eGameObjectType.ITEM, Properties.WORLD_OBJECT_UPDATE_INTERVAL, tick);
            UpdateDoors(player, tick);
            UpdateObjects(player, eGameObjectType.KEEP_COMPONENT, Properties.WORLD_OBJECT_UPDATE_INTERVAL, tick); // Unsure if needed.
            UpdateHouses(player, tick);
            player.LastWorldUpdate = tick;
        }

        private static void UpdateObjects(GamePlayer player, eGameObjectType objectType, uint updateInterval, long tick)
        {
            HashSet<GameObject> objectsInRange = player.CurrentRegion.GetInRadius<GameObject>(player, objectType, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameObject, long> objectCache = player.ObjectUpdateCaches[(byte) objectType];

            foreach (var objectInCache in objectCache)
            {
                GameObject gameObject = objectInCache.Key;

                if (!objectsInRange.Contains(gameObject) || !gameObject.IsVisibleTo(player))
                    objectCache.Remove(gameObject, out _);
            }

            foreach (GameObject objectInRange in objectsInRange)
            {
                if (!objectInRange.IsVisibleTo(player))
                    continue;

                if (!objectCache.TryGetValue(objectInRange, out long lastUpdate))
                    UpdateObjectForPlayer(player, objectInRange);
                else if (lastUpdate + updateInterval < tick)
                    UpdateObjectForPlayer(player, objectInRange);
            }
        }

        private static void UpdateDoors(GamePlayer player, long tick)
        {
            HashSet<GameDoorBase> doorsInRange = player.CurrentRegion.GetInRadius<GameDoorBase>(player, eGameObjectType.DOOR, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameObject, long> doorCache = player.ObjectUpdateCaches[(byte) eGameObjectType.DOOR];

            foreach (var doorInCache in doorCache)
            {
                GameDoorBase door = (GameDoorBase) doorInCache.Key;

                if (!doorsInRange.Contains(door) || !door.IsVisibleTo(player))
                    doorCache.Remove(door, out _);
            }

            foreach (GameDoorBase doorInRange in doorsInRange)
            {
                if (!doorInRange.IsVisibleTo(player))
                    continue;

                if (!doorCache.TryGetValue(doorInRange, out long lastUpdate))
                {
                    player.Out.SendObjectCreate(doorInRange);
                    player.Out.SendDoorState(doorInRange.CurrentRegion, doorInRange);
                    UpdateObjectForPlayer(player, doorInRange);
                }
                else if (lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL < tick)
                    UpdateObjectForPlayer(player, doorInRange);
            }
        }

        private static void UpdateHouses(GamePlayer player, long tick)
        {
            if (player.CurrentRegion == null || !player.CurrentRegion.HousingEnabled)
                return;

            ICollection<House> houses = HouseMgr.GetHouses(player.CurrentRegionID).Values;

            foreach (var houseEntry in player.HouseUpdateCache)
            {
                House house = houseEntry.Key;

                if (!houses.Contains(house) || !player.IsWithinRadius(house, HousingConstants.HouseViewingDistance))
                    player.HouseUpdateCache.Remove(house, out _);
            }

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
                else if (lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL < tick)
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
            }
        }
    }
}
