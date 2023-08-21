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

        public static void Tick()
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
                    if (player.LastWorldUpdate + Properties.WORLD_PLAYER_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                    {
                        long startTick = GameLoop.GetCurrentTime();
                        UpdateWorld(player);
                        long stopTick = GameLoop.GetCurrentTime();

                        if (stopTick - startTick > 25)
                            log.Warn($"Long {SERVICE_NAME}.{nameof(Tick)} for {player.Name}({player.ObjectID}) Time: {stopTick - startTick}ms");
                    }

                    player.movementComponent.Tick(GameLoop.GameLoopTime);
                }
                catch (Exception e)
                {
                    ServiceUtils.HandleServiceException(e, SERVICE_NAME, player, player);
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

        private static void AddObjectToPlayerCache(GamePlayer player, GameObject gameObject)
        {
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
            HashSet<GameNPC> npcsInRange = player.CurrentRegion.GetInRadius<GameNPC>(player, eGameObjectType.NPC, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameNPC, CachedNpcValues> npcUpdateCache = player.NpcUpdateCache;

            foreach (var npcInCache in npcUpdateCache)
            {
                GameNPC npc = npcInCache.Key;

                if (!npcsInRange.Contains(npc) || !npc.IsVisibleTo(player))
                    npcUpdateCache.Remove(npc, out _);
            }

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
            HashSet<GameStaticItem> itemsInRange = player.CurrentRegion.GetInRadius<GameStaticItem>(player, eGameObjectType.ITEM, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameStaticItem, long> itemUpdateCache = player.ItemUpdateCache;

            foreach (var itemInCache in itemUpdateCache)
            {
                GameStaticItem item = itemInCache.Key;

                if (!itemsInRange.Contains(item) || !item.IsVisibleTo(player))
                    itemUpdateCache.Remove(item, out _);
            }

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
            HashSet<GameDoorBase> doorsInRange = player.CurrentRegion.GetInRadius<GameDoorBase>(player, eGameObjectType.DOOR, WorldMgr.VISIBILITY_DISTANCE, false);
            ConcurrentDictionary<GameDoorBase, long> doorUpdateCache = player.DoorUpdateCache;

            foreach (var doorInCache in doorUpdateCache)
            {
                GameDoorBase door = doorInCache.Key;

                if (!doorsInRange.Contains(door) || !door.IsVisibleTo(player))
                    doorUpdateCache.Remove(door, out _);
            }

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
                else if (lastUpdate + Properties.WORLD_OBJECT_UPDATE_INTERVAL < GameLoop.GameLoopTime)
                    player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
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
