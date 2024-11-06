using System;
using System.Collections;
using System.Reflection;
using DOL.Database;
using DOL.GS.Quests;
using DOL.GS.ServerRules;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerRegionChangeRequest, "Player Region Change Request handler.", eClientStatus.PlayerInGame)]
    public class PlayerRegionChangeRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Holds jump point types
        /// </summary>
        protected readonly Hashtable m_customJumpPointHandlers = new();

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            ushort zonePointId = client.Version >= GameClient.eClientVersion.Version1126 ? packet.ReadShortLowEndian() : packet.ReadShort();
            eRealm playerRealm = client.Player.Realm;

            // If we are in TrialsOfAtlantis then base the target jump on the current region realm instead of the players realm.
            // This is only used if zone table has the proper realms defined, otherwise it reverts to old behavior.
            if (client.Player.CurrentRegion.Expansion == (int) eClientExpansion.TrialsOfAtlantis && client.Player.CurrentZone.Realm != eRealm.None)
                playerRealm = client.Player.CurrentZone.Realm;

            WhereClause whereClause = DB.Column("Id").IsEqualTo(zonePointId);

            if (client.Account.PrivLevel == 1)
            {
                WhereClause realmFilter = DB.Column("Realm").IsEqualTo((byte) playerRealm).Or(DB.Column("Realm").IsEqualTo(0)).Or(DB.Column("Realm").IsNull());
                whereClause = whereClause.And(realmFilter);
            }

            DbZonePoint zonePoint = DOLDB<DbZonePoint>.SelectObject(whereClause);

            if (zonePoint == null)
            {
                ChatUtil.SendDebugMessage(client, $"Invalid ZonePoint. Wrong ID or mismatching realm. (ID: {zonePointId}) (playerRealm: {(byte) playerRealm})");
                return;
            }

            // Some jump points are handled code side, such as instances. As such, region may be zero in the database.
            if (zonePoint.TargetRegion == 0)
            {
                zonePoint = new()
                {
                    Id = zonePointId
                };
            }

            if (client.Account.PrivLevel > 1)
                ChatUtil.SendDebugMessage(client, $"ZonePoint (ID: {zonePointId}) (TargetRegion: {zonePoint.TargetRegion}) (ClassType: {zonePoint.ClassType})");

            Region region = WorldMgr.GetRegion(zonePoint.TargetRegion);

            if (zonePoint.TargetRegion != 0)
            {
                if (region != null)
                {
                    if (client.Player.CurrentRegion.IsCustom == false && region.IsDisabled)
                    {
                        if (client.Player.Mission is not TaskDungeonMission taskDungeonMission || taskDungeonMission.TaskRegion.Skin != region.Skin)
                        {
                            client.Out.SendMessage("This region has been disabled!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                            if (client.Account.PrivLevel == 1)
                                return;
                        }
                    }
                }
            }

            // Allow the region to either deny exit or handle the zonepoint in a custom way.
            if (client.Player.CurrentRegion.OnZonePoint(client.Player, zonePoint) == false)
                return;

            DbBattleground bg = GameServer.KeepManager.GetBattleground(zonePoint.TargetRegion);

            if (bg != null && client.Player.Level < bg.MinLevel && client.Player.Level > bg.MaxLevel && client.Player.RealmLevel >= bg.MaxRealmLevel)
                return;

            IJumpPointHandler customHandler = null;

            if (!string.IsNullOrEmpty(zonePoint.ClassType))
            {
                customHandler = (IJumpPointHandler) m_customJumpPointHandlers[zonePoint.ClassType];

                // Check for db change to update cached handler.
                if (customHandler != null && customHandler.GetType().FullName != zonePoint.ClassType)
                    customHandler = null;

                if (customHandler == null)
                {
                    // Instances need to use a special handler. This is because some instances will result in duplicated zonepoints, such as if Tir Na Nog were to be instanced for a quest.
                    string typeName = client.Player.CurrentRegion.IsInstance ? "DOL.GS.ServerRules.InstanceDoorJumpPoint" : zonePoint.ClassType;
                    Type type = ScriptMgr.GetType(typeName);

                    if (type == null)
                        Log.Error($"ZonePoint not found (ID: {zonePoint.Id}) (Class {typeName})");
                    else if (!typeof(IJumpPointHandler).IsAssignableFrom(type))
                        Log.Error($"ZonePoint must implement {nameof(IJumpPointHandler)} interface (ID: {zonePoint.Id}) (Class {typeName})");
                    else
                    {
                        try
                        {
                            customHandler = (IJumpPointHandler) Activator.CreateInstance(type);
                        }
                        catch (Exception e)
                        {
                            customHandler = null;
                            Log.Error($"Error when creating a new instance of jump point handler (ID: {zonePoint.Id}) (Class {typeName})", e);
                        }
                    }
                }

                if (customHandler != null)
                    m_customJumpPointHandlers[zonePoint.ClassType] = customHandler;
            }

            GamePlayer player = client.Player;

            if (region != null && (GameClient.eClientType) region.Expansion > player.Client.ClientType)
            {
                player.Out.SendMessage($"Destination region {region.Description} is not supported by your client type.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            try
            {
                // Check if the zone point has source locations set  Check prior to any zone point modification by handlers.
                if (zonePoint.SourceRegion == 0)
                {
                    zonePoint.SourceRegion = player.CurrentRegionID;
                    zonePoint.SourceX = player.X;
                    zonePoint.SourceY = player.Y;
                    zonePoint.SourceZ = player.Z;
                    GameServer.Database.SaveObject(zonePoint);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Can't save updated ZonePoint with source info.", ex);
            }

            if (customHandler != null)
            {
                try
                {
                    if (!customHandler.IsAllowedToJump(zonePoint, player))
                        return;
                }
                catch (Exception e)
                {
                    if (Log.IsErrorEnabled)
                        Log.Error($"Jump point handler ({zonePoint.ClassType})", e);

                    player.Out.SendMessage($"exception in jump point ({zonePoint.Id}) handler", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return;
                }
            }

            player.MoveTo(zonePoint.TargetRegion, zonePoint.TargetX, zonePoint.TargetY, zonePoint.TargetZ, zonePoint.TargetHeading);
        }
    }
}
