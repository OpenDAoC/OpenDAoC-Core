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
        private const string SERVICE_NAME = "PlayerService";

        public static void Tick(long tick)
        {
            GameLoop.CurrentServiceTick = SERVICE_NAME;
            Diagnostics.StartPerfCounter(SERVICE_NAME);

            List<GamePlayer> list = EntityManager.UpdateAndGetAll<GamePlayer>(EntityManager.EntityType.Player, out int lastNonNullIndex);

            Parallel.For(0, lastNonNullIndex + 1, i =>
            {
                GamePlayer player = list[i];

                if (player == null||
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

                        if ((stopTick - startTick) > 25)
                            log.Warn($"Long UpdateWorld for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
                    }

                    player.movementComponent.Tick(tick);
                }
                catch (Exception e)
                {
                    log.Error($"Critical error encountered: {e}");
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
            UpdateObjects(player, eGameObjectType.DOOR, Properties.WORLD_OBJECT_UPDATE_INTERVAL, tick);
            UpdateObjects(player, eGameObjectType.KEEP_COMPONENT, Properties.WORLD_OBJECT_UPDATE_INTERVAL, tick);
            UpdateHouses(player, tick);
            player.LastWorldUpdate = tick;
        }

        private static void UpdateObjects(GamePlayer player, eGameObjectType objectType, uint updateInterval, long tick)
        {
            HashSet<GameObject> objectsInRange = player.CurrentRegion.GetInRadius<GameObject>(player, objectType, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameObject, long> objectsCache = player.ObjectUpdateCaches[(byte) objectType];

            foreach (var objectInCache in objectsCache)
            {
                GameObject gameObject = objectInCache.Key;

                if (!objectsInRange.Contains(gameObject) || !gameObject.IsVisibleTo(player))
                    objectsCache.Remove(gameObject, out _);
            }

            foreach (GameObject objectInRange in objectsInRange)
            {
                if (!objectInRange.IsVisibleTo(player))
                    continue;

                if (!objectsCache.TryGetValue(objectInRange, out long lastUpdate))
                    UpdateObjectForPlayer(player, objectInRange);
                else if (lastUpdate + updateInterval < tick)
                    UpdateObjectForPlayer(player, objectInRange);
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
