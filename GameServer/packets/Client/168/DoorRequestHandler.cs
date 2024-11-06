using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.ServerProperties;
using DOL.Language;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.DoorRequest, "Door Interact Request Handler", eClientStatus.PlayerInGame)]
    public class DoorRequestHandler : IPacketHandler
    {
        public static int HandlerDoorId { get; private set; }

        /// <summary>
        /// door index which is unique
        /// </summary>
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int doorId = (int) packet.ReadInt();
            HandlerDoorId = doorId;
            byte doorState = (byte) packet.ReadByte();
            int doorType = doorId / 100000000;
            int radius = Properties.WORLD_PICKUP_DISTANCE * 4;
            int zoneDoor = doorId / 1000000;
            string debugText = string.Empty;

            // For ToA, the client always sends the same ID, so we need to construct an ID using the current zone.
            if ((eClientExpansion) client.Player.CurrentRegion.Expansion is eClientExpansion.TrialsOfAtlantis)
            {
                debugText = $"ToA DoorID:{doorId} ";
                doorId -= zoneDoor * 1000000;
                zoneDoor = client.Player.CurrentZone.ID;
                doorId += zoneDoor * 1000000;
                HandlerDoorId = doorId;

                // Experimental to handle a few odd TOA door issues.
                if (client.Player.CurrentRegion.IsDungeon)
                    radius *= 4;
            }

            // Debug text.
            if (client.Account.PrivLevel > 1 || Properties.ENABLE_DEBUG)
            {
                if (doorType == 7)
                {
                    int ownerKeepId = doorId / 100000 % 1000;
                    int towerNum = doorId / 10000 % 10;
                    int keepID = ownerKeepId + towerNum * 256;
                    int componentID = doorId / 100 % 100;
                    int doorIndex = doorId % 10;
                    client.Out.SendDebugMessage($"Keep Door ID:{doorId} state:{doorState} (Owner Keep:{ownerKeepId} KeepID:{keepID} ComponentID:{componentID} DoorIndex:{doorIndex} TowerNumber:{towerNum})");

                    if (keepID > 255 && ownerKeepId < 10)
                        ChatUtil.SendDebugMessage(client, "Warning: Towers with an Owner Keep ID < 10 will have untargetable doors!");
                }
                else if (doorType == 9)
                {
                    int doorIndex = doorId - doorType * 10000000;
                    client.Out.SendDebugMessage($"House DoorID:{doorId} state:{doorState} (doorType:{doorType} doorIndex:{doorIndex})");
                }
                else
                {
                    int fixture = doorId - zoneDoor * 1000000;
                    int fixturePiece = fixture;
                    fixture /= 100;
                    fixturePiece -= fixture * 100;
                    client.Out.SendDebugMessage($"{debugText}DoorID:{doorId} state:{doorState} zone:{zoneDoor} fixture:{fixture} fixturePiece:{fixturePiece} Type:{doorType}");
                }
            }

            if (client.Player.TargetObject is GameDoor && !client.Player.IsWithinRadius(client.Player.TargetObject, radius))
            {
                client.Player.Out.SendMessage("You are too far to open this door", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                return;
            }

            DbDoor door = DOLDB<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(doorId));

            if (door != null)
            {
                if (doorType is 7 or 9)
                {
                    UseDoor();
                    return;
                }

                if (client.Account.PrivLevel == 1)
                {
                    if (door.Locked == 0)
                    {
                        if (door.Health == 0)
                        {
                            UseDoor();
                            return;
                        }

                        if (GameServer.Instance.Configuration.ServerType is EGameServerType.GST_PvP or EGameServerType.GST_PvE)
                        {
                            if (door.Realm != 0)
                            {
                                UseDoor();
                                return;
                            }
                        }
                        else
                        {
                            if (client.Player.Realm == (eRealm) door.Realm || door.Realm == 6)
                            {
                                UseDoor();
                                return;
                            }
                        }
                    }
                }
                else if (client.Account.PrivLevel > 1)
                {
                    client.Out.SendDebugMessage("GM: Forcing locked door open. ");
                    client.Out.SendDebugMessage($"PosternDoor: {door.IsPostern}");

                    UseDoor();
                    return;
                }
            }
            else
            {
                if (doorType != 9 && client.Account.PrivLevel > 1 && client.Player.CurrentRegion.IsInstance == false)
                {
                    if (client.Player.TempProperties.GetProperty<bool>(DoorMgr.WANT_TO_ADD_DOORS))
                        client.Player.Out.SendCustomDialog("This door is not in the database. Place yourself nearest to this door and click Accept to add it.", AddDoor);
                    else
                        client.Player.Out.SendMessage("This door is not in the database. Use '/door show' to enable the add door dialog when targeting doors.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
                }

                UseDoor();
                return;
            }

            void UseDoor()
            {
                GamePlayer player = client.Player;
                List<GameDoorBase> doorList = DoorMgr.getDoorByID(doorId);

                if (doorList.Count > 0)
                {
                    bool success = false;

                    foreach (GameDoorBase door in doorList)
                    {
                        if (success)
                            break;

                        if (door is GameKeepDoor)
                        {
                            GameKeepDoor keepDoor = door as GameKeepDoor;

                            if (keepDoor.Component.Keep is GameKeepTower && keepDoor.Component.Keep.KeepComponents.Count > 1)
                                keepDoor.Interact(player);

                            success = true;
                        }
                        else
                        {
                            if (player.IsWithinRadius(door, radius))
                            {
                                if (doorState == 0x01)
                                    door.Open(player);
                                else
                                    door.Close(player);

                                success = true;
                            }
                        }
                    }

                    if (!success)
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DoorRequestHandler.OnTick.TooFarAway", doorList[0].Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
                else
                {
                    // New frontiers. We don't want this (relic gates, etc).
                    if (player.CurrentRegionID == 163 && player.Client.Account.PrivLevel == 1)
                        return;

                    player.Out.SendDebugMessage($"Door {doorId} not found in door list, opening via GM door hack.");

                    GameDoor door = new()
                    {
                        DoorID = doorId,
                        X = player.X,
                        Y = player.Y,
                        Z = player.Z,
                        Realm = eRealm.Door,
                        CurrentRegion = player.CurrentRegion
                    };

                    if (player.IsWithinRadius(door, radius))
                        door.Open(player);
                    else
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "DoorRequestHandler.OnTick.TooFarAway", doorList[0].Name), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                }
            }
        }

        public static void AddDoor(GamePlayer player, byte response)
        {
            if (response != 0x01)
                return;

            int doorType = HandlerDoorId / 100000000;

            if (doorType == 7)
                PositionMgr.CreateDoor(HandlerDoorId, player);
            else
            {
                DbDoor door = new()
                {
                    ObjectId = null,
                    InternalID = HandlerDoorId,
                    Name = "door",
                    Type = HandlerDoorId / 100000000,
                    Level = 20,
                    Realm = 6,
                    X = player.X,
                    Y = player.Y,
                    Z = player.Z,
                    Heading = player.Heading
                };

                GameServer.Database.AddObject(door);
                player.Out.SendMessage($"Added door {HandlerDoorId} to the database!", eChatType.CT_Important,eChatLoc.CL_SystemWindow);
                GameServer.Database.SaveObject(door);
                DoorMgr.Init();
            }
        }
    }
}
