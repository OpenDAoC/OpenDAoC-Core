using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DOL.AI.Brain;
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

        private static void UpdateWorld(GamePlayer player, long tick)
        {
            if (Properties.WORLD_NPC_UPDATE_INTERVAL > 0)
                UpdateNpcs(player, tick);

            if (Properties.WORLD_OBJECT_UPDATE_INTERVAL > 0)
            {
                UpdateItems(player, tick);
                UpdateDoors(player, tick);
                UpdateHouses(player, tick);
            }

            player.LastWorldUpdate = tick;
        }

        private static void UpdateNpcs(GamePlayer player, long tick)
        {
            HashSet<GameNPC> npcsInRange = player.GetNPCsInRadius(WorldMgr.VISIBILITY_DISTANCE);

            try
            {
                // Clean up cache.
                foreach (var objEntry in player.Client.GameObjectUpdateArray)
                {
                    Tuple<ushort, ushort> objKey = objEntry.Key;

                    if (WorldMgr.GetRegion(objKey.Item1).GetObject(objKey.Item2) is not GameNPC npc)
                        continue;

                    // Brain is updating to its master, no need to handle it.
                    if (npc.Brain is IControlledBrain brain && brain.GetPlayerOwner() == player)
                        continue;

                    // We have a NPC in cache that is not in vincinity.
                    if (!npcsInRange.Contains(npc) && (tick - objEntry.Value) >= Properties.WORLD_NPC_UPDATE_INTERVAL)
                    {
                        // Update him out of view.
                        if (npc.IsVisibleTo(player))
                            player.Client.Out.SendObjectUpdate(npc);

                        // This will add the object to the cache again, remove it after sending.
                        player.Client.GameObjectUpdateArray.TryRemove(objKey, out _);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while cleaning NPC cache for player : {0}, Exception : {1}", player.Name, e);
            }

            try
            {
                // Now send remaining NPCs.
                foreach (GameNPC npc in npcsInRange)
                {
                    if (!npc.IsVisibleTo(player))
                        continue;

                    if (player.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(npc.CurrentRegionID, (ushort) npc.ObjectID), out long lastUpdate))
                    {
                        if ((tick - lastUpdate) >= Properties.WORLD_NPC_UPDATE_INTERVAL)
                            player.Client.Out.SendObjectUpdate(npc);
                    }
                    else
                    {
                        // NPC in range. Not in cache yet. Sending update will add it to the cache.
                        player.Client.Out.SendObjectUpdate(npc);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while updating NPCs for player : {0}, Exception : {1}", player.Name, e);
            }
        }

        private static void UpdateItems(GamePlayer player, long tick)
        {
            HashSet<GameStaticItem> itemsInRange = player.GetItemsInRadius(WorldMgr.OBJ_UPDATE_DISTANCE);

            try
            {
                // Clean up cache.
                foreach (var objEntry in player.Client.GameObjectUpdateArray)
                {
                    Tuple<ushort, ushort> objKey = objEntry.Key;

                    // We have an item in cache that is not in vincinity.
                    if (WorldMgr.GetRegion(objKey.Item1).GetObject(objKey.Item2) is GameStaticItem item && !itemsInRange.Contains(item) && (tick - objEntry.Value) >= Properties.WORLD_OBJECT_UPDATE_INTERVAL)
                        player.Client.GameObjectUpdateArray.TryRemove(objKey, out _);
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while cleaning static item cache for player : {0}, Exception : {1}", player.Name, e);
            }

            try
            {
                // Now send remaining items.
                foreach (GameStaticItem item in itemsInRange)
                {
                    if (!item.IsVisibleTo(player))
                        continue;

                    if (player.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(item.CurrentRegionID, (ushort) item.ObjectID), out long lastUpdate))
                    {
                        if ((tick - lastUpdate) >= Properties.WORLD_OBJECT_UPDATE_INTERVAL)
                            player.Client.Out.SendObjectCreate(item);
                    }
                    else
                    {
                        // Item in range. Not in cache yet. Sending update will add it to the cache.
                        player.Client.Out.SendObjectCreate(item);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while updating static items for player : {0}, Exception : {1}", player.Name, e);
            }
        }

        private static void UpdateDoors(GamePlayer player, long tick)
        {
            HashSet<GameDoorBase> doorsInRange = player.GetDoorsInRadius(WorldMgr.OBJ_UPDATE_DISTANCE);

            try
            {
                // Clean up cache.
                foreach (var objEntry in player.Client.GameObjectUpdateArray)
                {
                    Tuple<ushort, ushort> objKey = objEntry.Key;

                    // We have a door in cache that is not in vincinity.
                    if (WorldMgr.GetRegion(objKey.Item1).GetObject(objKey.Item2) is GameDoorBase door && !doorsInRange.Contains(door) && (tick - objEntry.Value) >= Properties.WORLD_OBJECT_UPDATE_INTERVAL)
                        player.Client.GameObjectUpdateArray.TryRemove(objKey, out _);
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while cleaning door cache for player : {0}, Exception : {1}", player.Name, e);
            }

            try
            {
                // Now send remaining doors
                foreach (GameDoorBase door in doorsInRange)
                {
                    if (!door.IsVisibleTo(player))
                        continue;

                    if (player.Client.GameObjectUpdateArray.TryGetValue(new Tuple<ushort, ushort>(door.CurrentRegionID, (ushort) door.ObjectID), out long lastUpdate))
                    {
                        if ((tick - lastUpdate) >= Properties.WORLD_OBJECT_UPDATE_INTERVAL)
                            player.SendDoorUpdate(door);
                    }
                    else
                    {
                        // Door in range. Not in cache yet. Sending update will add it to the cache.
                        player.SendDoorUpdate(door);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while updating doors for player : {0}, Exception : {1}", player.Name, e);
            }
        }

        private static void UpdateHouses(GamePlayer player, long tick)
        {
            if (player.CurrentRegion == null || !player.CurrentRegion.HousingEnabled)
                return;

            ICollection<House> houses = HouseMgr.GetHouses(player.CurrentRegionID).Values;

            try
            {
                // Clean up cache.
                foreach (var houseEntry in player.Client.HouseUpdateArray)
                {
                    Tuple<ushort, ushort> houseKey = houseEntry.Key;
                    House house = HouseMgr.GetHouse(houseKey.Item1, houseKey.Item2);

                    // We have a House in cache that is not in vincinity.
                    if (!houses.Contains(house) && (tick - houseEntry.Value) >= (Properties.WORLD_OBJECT_UPDATE_INTERVAL >> 2))
                        player.Client.HouseUpdateArray.TryRemove(houseKey, out _);
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while cleaning house cache for player : {0}, Exception : {1}", player.Name, e);
            }

            try
            {
                foreach (House house in houses)
                {
                    if (!player.IsWithinRadius(house, HousingConstants.HouseViewingDistance))
                        continue;

                    if (player.Client.HouseUpdateArray.TryGetValue(new Tuple<ushort, ushort>(house.RegionID, (ushort) house.HouseNumber), out long lastUpdate))
                    {
                        if ((tick - lastUpdate) >= Properties.WORLD_OBJECT_UPDATE_INTERVAL)
                            player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
                    }
                    else
                    {
                        // House in range. Not in cache yet. Sending update will add it to the cache.
                        player.Client.Out.SendHouse(house);
                        player.Client.Out.SendGarden(house);
                        player.Client.Out.SendHouseOccupied(house, house.IsOccupied);
                    }
                }
            }
            catch (Exception e)
            {
                if (log.IsErrorEnabled)
                    log.ErrorFormat("Error while updating houses for player : {0}, Exception : {1}", player.Name, e);
            }
        }
    }
}
