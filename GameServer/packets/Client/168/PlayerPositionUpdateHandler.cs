using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using DOL.Database;
using DOL.GS.Utils;
using DOL.Language;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PositionUpdate, "Handles player position updates", eClientStatus.PlayerInGame)]
    public class PlayerPositionUpdateHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Stores the count of times the player is above speedhack tolerance!
        /// If this value reaches 10 or more, a logfile entry is written.
        /// </summary>
        private const string SHSPEEDCOUNTER = "SHSPEEDCOUNTER";
        private const string SHLASTUPDATETICK = "SHLASTUPDATETICK";
        private const string SHLASTFLY = "SHLASTFLY";
        private const string SHLASTSTATUS = "SHLASTSTATUS";
        private const string LASTCPSTICK = "LASTCPSTICK";

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            //Tiv: in very rare cases client send 0xA9 packet before sending S<=C 0xE8 player world initialize
            if (client.Player.ObjectState is not GameObject.eObjectState.Active || client.ClientState is not GameClient.eClientState.Playing)
                return;

            if (!client.Player.OnPositionPacketReceivedStart())
                return;

            HandlePacketInternal(client, packet);
            client.Player.OnPositionPacketReceivedEnd();
        }

        private static void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            // In very rare cases client send 0xA9 packet before sending S<=C 0xE8 player world initialize
            if (client.Player.ObjectState is not GameObject.eObjectState.Active || client.ClientState is not GameClient.eClientState.Playing and not GameClient.eClientState.Linkdead)
                return;

            // Don't allow movement if the player isn't close to the NPC they're supposed to be riding.
            // Instead, teleport them to it and send an update packet (the client may then ask for a create packet).
            if (client.Player.Steed != null && client.Player.Steed.ObjectState is GameObject.eObjectState.Active)
            {
                GamePlayer rider = client.Player;
                GameNPC steed = rider.Steed;

                // The rider and their steed are never at the exact same position (made worse by a high latency).
                // So the radius is arbitrary and must not be too low to avoid spamming packets.
                if (!rider.IsWithinRadius(steed, 1000))
                {
                    rider.X = steed.X;
                    rider.Y = steed.Y;
                    rider.Z = steed.Z;
                    rider.Heading = steed.Heading;
                    rider.Out.SendPlayerJump(false);
                    return;
                }
            }

            if (client.Version >= GameClient.eClientVersion.Version1124)
                HandlePacketSince1124(client, packet);
            else
                HandlePacketPre1124(client, packet); // This is very outdated and is no longer working.

            static void HandlePacketSince1124(GameClient client, GSPacketIn packet)
            {
                float x = packet.ReadFloatLowEndian();
                float y = packet.ReadFloatLowEndian();
                float z = packet.ReadFloatLowEndian();
                float speed = packet.ReadFloatLowEndian();
                float zSpeed = packet.ReadFloatLowEndian();
                packet.Skip(2); // Session ID.

                if (client.Version >= GameClient.eClientVersion.Version1127)
                    packet.Skip(2); // Object ID.

                ushort zoneId = packet.ReadShort();

                if (!ProcessStateFlags(client.Player, (StateFlags) packet.ReadByte()))
                    return;

                packet.Skip(1); // Unknown.
                ushort fallingDamage = packet.ReadShort();
                ushort heading = packet.ReadShort();
                ProcessActionFlags(client.Player, (ActionFlags) packet.ReadByte());
                packet.Skip(2); // Unknown bytes.
                packet.Skip(1); // Health.
                // two trailing bytes, no data, +2 more for 1.127+.

                if ((client.Player.IsMezzed || client.Player.IsStunned) && !client.Player.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound))
                    client.Player.CurrentSpeed = 0;
                else
                {
                    if (client.Player.CurrentSpeed == 0 &&
                        (client.Player.LastPositionUpdatePoint.X != x || client.Player.LastPositionUpdatePoint.Y != y))
                    {
                        if (client.Player.IsSitting)
                            client.Player.Sit(false);
                    }

                    client.Player.CurrentSpeed = (short) speed;
                }

                client.Player.FallSpeed = (short) zSpeed;

                Zone newZone = WorldMgr.GetZone(zoneId);

                if (newZone == null)
                {
                    if (!client.Player.TempProperties.GetProperty<bool>("isbeingbanned"))
                    {
                        log.Error($"{client.Player.Name}'s position in unknown zone! => {zoneId}");
                        GamePlayer player = client.Player;
                        player.TempProperties.SetProperty("isbeingbanned", true);
                        player.MoveToBind();
                    }

                    return; // TODO: what should we do? player lost in space
                }

                // move to bind if player fell through the floor
                if (z == 0)
                {
                    client.Player.MoveToBind();
                    return;
                }

                bool zoneChange = newZone != client.Player.LastPositionUpdateZone;

                if (zoneChange)
                {
                    //If the region changes -> make sure we don't take any falling damage
                    if (client.Player.LastPositionUpdateZone != null && newZone.ZoneRegion.ID != client.Player.LastPositionUpdateZone.ZoneRegion.ID)
                        client.Player.MaxLastZ = int.MinValue;

                    /*
                     * "You have entered Burial Tomb."
                     * "Burial Tomb"
                     * "Current area is adjusted for one level 1 player."
                     * "Current area has a 50% instance bonus."
                     */

                    string description = newZone.Description;
                    string screenDescription = description;

                    if (LanguageMgr.GetTranslation(client, newZone) is DbLanguageZone translation)
                    {
                        if (!string.IsNullOrEmpty(translation.Description))
                            description = translation.Description;

                        if (!string.IsNullOrEmpty(translation.ScreenDescription))
                            screenDescription = translation.ScreenDescription;
                    }

                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPositionUpdateHandler.Entered", description), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(screenDescription, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
                    client.Player.LastPositionUpdateZone = newZone;

                    if (client.Player.GMStealthed)
                        client.Player.Stealth(true);
                }

                int coordsPerSec = 0;
                int jumpDetect = 0;
                long timediff = GameLoop.GameLoopTime - client.Player.LastPositionUpdatePacketReceivedTime;
                int distance = 0;

                if (timediff > 0)
                {
                    Point3D newPoint = new(x, y, z);
                    distance = newPoint.GetDistanceTo(new Point3D((int) client.Player.LastPositionUpdatePoint.X, (int) client.Player.LastPositionUpdatePoint.Y, (int)client.Player.LastPositionUpdatePoint.Z));
                    coordsPerSec = distance * 1000 /(int)timediff;

                    if (distance < 100 && client.Player.LastPositionUpdatePoint.Z > 0)
                        jumpDetect = (int) (z - client.Player.LastPositionUpdatePoint.Z);
                }

                if (distance > 0)
                    client.Player.LastPlayerActivityTime = GameLoop.GameLoopTime;

                client.Player.LastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;
                client.Player.LastPositionUpdatePoint.X = x;
                client.Player.LastPositionUpdatePoint.Y = y;
                client.Player.LastPositionUpdatePoint.Z = z;
                int tolerance = ServerProperties.Properties.CPS_TOLERANCE;

                if (client.Player.movementComponent.MaxSpeedPercent > 0)
                {
                    if (client.Player.Steed != null)
                        tolerance += client.Player.Steed.MaxSpeed;
                    else
                        tolerance += client.Player.MaxSpeed;
                }

                if (client.Player.IsJumping)
                {
                    coordsPerSec = 0;
                    jumpDetect = 0;
                    client.Player.IsJumping = false;
                }

                if (!client.Player.IsAllowedToFly && (coordsPerSec > tolerance || jumpDetect > ServerProperties.Properties.JUMP_TOLERANCE))
                {
                    bool isHackDetected = true;

                    if (coordsPerSec > tolerance)
                    {
                        // check to see if CPS time tolerance is exceeded
                        int lastCPSTick = client.Player.TempProperties.GetProperty<int>(LASTCPSTICK);

                        if (GameLoop.GameLoopTime - lastCPSTick > ServerProperties.Properties.CPS_TIME_TOLERANCE)
                            isHackDetected = false;
                    }

                    if (isHackDetected)
                    {
                        StringBuilder builder = new();
                        builder.Append("MOVEHACK_DETECT");
                        builder.Append(": CharName=");
                        builder.Append(client.Player.Name);
                        builder.Append(" Account=");
                        builder.Append(client.Account.Name);
                        builder.Append(" IP=");
                        builder.Append(client.TcpEndpointAddress);
                        builder.Append(" CPS:=");
                        builder.Append(coordsPerSec);
                        builder.Append(" JT=");
                        builder.Append(jumpDetect);
                        ChatUtil.SendDebugMessage(client, builder.ToString());

                        if (client.Account.PrivLevel == 1)
                        {
                            GameServer.Instance.LogCheatAction(builder.ToString());

                            if (ServerProperties.Properties.ENABLE_MOVEDETECT)
                            {
                                if (ServerProperties.Properties.BAN_HACKERS && false) // banning disabled until this technique is proven accurate
                                {
                                    DbBans b = new()
                                    {
                                        Author = "SERVER",
                                        Ip = client.TcpEndpointAddress,
                                        Account = client.Account.Name,
                                        DateBan = DateTime.Now,
                                        Type = "B",
                                        Reason = string.Format("Autoban MOVEHACK:(CPS:{0}, JT:{1}) on player:{2}", coordsPerSec, jumpDetect, client.Player.Name)
                                    };

                                    GameServer.Database.AddObject(b);
                                    GameServer.Database.SaveObject(b);
                                    string message = "You have been auto kicked and banned due to movement hack detection!";

                                    for (int i = 0; i < 8; i++)
                                    {
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                    }

                                    client.Out.SendPlayerQuit(true);
                                    client.Player.SaveIntoDatabase();
                                    client.Player.Quit(true);
                                }
                                else
                                {
                                    string message = "You have been auto kicked due to movement hack detection!";

                                    for (int i = 0; i < 8; i++)
                                    {
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                    }

                                    client.Out.SendPlayerQuit(true);
                                    client.Player.SaveIntoDatabase();
                                    client.Player.Quit(true);
                                }

                                client.Disconnect();
                                return;
                            }
                        }
                    }

                    client.Player.TempProperties.SetProperty(LASTCPSTICK, GameLoop.GameLoopTime);
                }

                client.Player.X = (int) x;
                client.Player.Y = (int) y;
                client.Player.Z = (int) z;
                client.Player.Heading = heading;

                // Area.
                if (client.Player.CurrentRegion.Time > client.Player.AreaUpdateTick)
                {
                    IList<IArea> oldAreas = client.Player.CurrentAreas;

                    // Because we may be in an instance we need to do the area check from the current region
                    // rather than relying on the zone which is in the skinned region.  - Tolakram

                    IList<IArea> newAreas = client.Player.CurrentRegion.GetAreasOfZone(newZone, client.Player);

                    // Check for left areas
                    if (oldAreas != null)
                    {
                        foreach (IArea area in oldAreas)
                        {
                            if (!newAreas.Contains(area))
                            {
                                area.OnPlayerLeave(client.Player);

                                // Check if leaving Border Keep areas so we can check RealmTimer.
                                // This is very ugly.
                                if (area is AbstractArea borderKeep &&
                                    (borderKeep.Description.Equals("Castle Sauvage") ||
                                    borderKeep.Description.Equals("Snowdonia Fortress") ||
                                    borderKeep.Description.Equals("Svasud Faste") ||
                                    borderKeep.Description.Equals("Vindsaul Faste") ||
                                    borderKeep.Description.Equals("Druim Ligen") ||
                                    borderKeep.Description.Equals("Druim Cain")))
                                {
                                    RealmTimer.CheckRealmTimer(client.Player);
                                }
                            }
                        }
                    }

                    // Check for entered areas
                    foreach (IArea area in newAreas)
                    {
                        if (oldAreas == null || !oldAreas.Contains(area))
                            area.OnPlayerEnter(client.Player);
                    }

                    // set current areas to new one...
                    client.Player.CurrentAreas = newAreas;
                    client.Player.AreaUpdateTick = client.Player.CurrentRegion.Time + 2000; // update every 2 seconds
                }

                GameLocation[] locations = client.Player.LastUniqueLocations;
                GameLocation loc = locations[0];

                if (loc.X != (int) x || loc.Y != (int) y || loc.Z != (int) z || loc.RegionID != client.Player.CurrentRegionID)
                {
                    loc = locations[^1];
                    Array.Copy(locations, 0, locations, 1, locations.Length - 1);
                    locations[0] = loc;
                    loc.X = (int) x;
                    loc.Y = (int) y;
                    loc.Z = (int) z;
                    loc.Heading = client.Player.Heading;
                    loc.RegionID = client.Player.CurrentRegionID;
                }

                // Fall damage.
                if (GameServer.ServerRules.CanTakeFallDamage(client.Player) && !client.Player.IsSwimming)
                {
                    try
                    {
                        int maxLastZ = client.Player.MaxLastZ;

                        // Are we on the ground?
                        if ((fallingDamage >> 15) != 0)
                        {
                            int safeFallLevel = client.Player.GetAbilityLevel(Abilities.SafeFall);
                            float fallSpeed = zSpeed * -1 - 100 * safeFallLevel;
                            int fallDivide = 15;
                            int fallPercent = (int) Math.Min(99, (fallSpeed - 501) / fallDivide);

                            if (fallSpeed > 500)
                                client.Player.CalcFallDamage(fallPercent);

                            client.Player.MaxLastZ = client.Player.Z;
                        }
                        else if (maxLastZ < client.Player.Z || client.Player.IsRiding || zSpeed > -150) // is riding, for dragonflys
                            client.Player.MaxLastZ = client.Player.Z;
                    }
                    catch
                    {
                        log.Warn("error when attempting to calculate fall damage");
                    }
                }

                if (client.Player.Steed != null && client.Player.Steed.ObjectState is GameObject.eObjectState.Active)
                    client.Player.Heading = client.Player.Steed.Heading;

                // Close trade window.
                if (client.Player.TradeWindow?.Partner != null && !client.Player.IsWithinRadius(client.Player.TradeWindow.Partner, WorldMgr.GIVE_ITEM_DISTANCE))
                    client.Player.TradeWindow.CloseTrade();
            }

            static void HandlePacketPre1124(GameClient client, GSPacketIn packet)
            {
                long environmentTick = GameLoop.GameLoopTime;
                packet.Skip(2); //PID
                ushort speedData = packet.ReadShort();
                int speed = speedData & 0x1FF;

                if ((speedData & 0x200) != 0)
                    speed = -speed;

                if ((client.Player.IsMezzed || client.Player.IsStunned) && !client.Player.effectListComponent.ContainsEffectForEffectType(eEffect.SpeedOfSound))
                    // Nidel: updating client.Player.CurrentSpeed instead of speed
                    client.Player.CurrentSpeed = 0;
                else
                    client.Player.CurrentSpeed = (short) speed;

                client.Player.IsStrafing = (speedData & 0xe000) != 0;
                int realZ = packet.ReadShort();
                ushort xOffsetInZone = packet.ReadShort();
                ushort yOffsetInZone = packet.ReadShort();
                ushort zoneId = packet.ReadShort();

                try
                {
                    Zone grabZone = WorldMgr.GetZone(zoneId);
                }
                catch (Exception)
                {
                    //if we get a zone that doesn't exist, move player to their bindstone
                    client.Player.MoveTo((ushort) client.Player.BindRegion,
                                          client.Player.BindXpos,
                                          client.Player.BindYpos,
                                          (ushort) client.Player.BindZpos,
                                          (ushort) client.Player.BindHeading);
                    return;
            
                }

                //Dinberg - Instance considerations.
                //Now this gets complicated, so listen up! We have told the client a lie when it comes to the zoneID.
                //As a result, every movement update, they are sending a lie back to us. Two liars could get confusing!

                //BUT, the lie we sent has a truth to it - the geometry and layout of the zone. As a result, the zones
                //x and y offsets will still actually be relevant to our current zone. And for the clones to have been
                //created, there must have been a real zone to begin with, of id == instanceZone.SkinID.

                //So, although our client is lying to us, and thinks its in another zone, that zone happens to coincide
                //exactly with the zone we are instancing - and so all the positions still ring true.

                //Philosophically speaking, its like looking in a mirror and saying 'Am I a reflected, or reflector?'
                //What it boils down to has no bearing whatsoever on the result of anything, so long as someone sitting
                //outside of the unvierse knows not to listen to whether you say which you are, and knows the truth to the
                //answer. Then, he need only know what you are doing ;)

                Zone newZone = WorldMgr.GetZone(zoneId);

                if (newZone == null)
                {
                    if (client.Player==null)
                        return;

                    if (!client.Player.TempProperties.GetProperty<bool>("isbeingbanned"))
                    {
                        if (log.IsErrorEnabled)
                            log.Error($"{client.Player.Name}'s position in unknown zone! => {zoneId}");

                        GamePlayer player=client.Player;
                        player.TempProperties.SetProperty("isbeingbanned", true);
                        player.MoveToBind();
                    }

                    return; // TODO: what should we do? player lost in space
                }

                // move to bind if player fell through the floor
                if (realZ == 0)
                {
                    client.Player.MoveTo((ushort)client.Player.BindRegion,
                                         client.Player.BindXpos,
                                         client.Player.BindYpos,
                                         (ushort)client.Player.BindZpos,
                                         (ushort)client.Player.BindHeading);
                    return;
                }

                int realX = newZone.XOffset + xOffsetInZone;
                int realY = newZone.YOffset + yOffsetInZone;
                bool zoneChange = newZone != client.Player.LastPositionUpdateZone;

                if (zoneChange)
                {
                    //If the region changes -> make sure we don't take any falling damage
                    if (client.Player.LastPositionUpdateZone != null && newZone.ZoneRegion.ID != client.Player.LastPositionUpdateZone.ZoneRegion.ID)
                        client.Player.MaxLastZ = int.MinValue;

                    // Update water level and diving flag for the new zone
                    // commenting this out for now, creates a race condition when teleporting within same region, jumping player back and forth as player xyz isnt updated yet.
                    //client.Out.SendPlayerPositionAndObjectID();		
                    zoneChange = true;

                    /*
                     * "You have entered Burial Tomb."
                     * "Burial Tomb"
                     * "Current area is adjusted for one level 1 player."
                     * "Current area has a 50% instance bonus."
                     */

                    string description = newZone.Description;
                    string screenDescription = description;

                    if (LanguageMgr.GetTranslation(client, newZone) is DbLanguageZone translation)
                    {
                        if (!string.IsNullOrEmpty(translation.Description))
                            description = translation.Description;

                        if (!string.IsNullOrEmpty(translation.ScreenDescription))
                            screenDescription = translation.ScreenDescription;
                    }

                    client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPositionUpdateHandler.Entered", description), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    client.Out.SendMessage(screenDescription, eChatType.CT_ScreenCenterSmaller, eChatLoc.CL_SystemWindow);
                    client.Player.LastPositionUpdateZone = newZone;

                    if (client.Player.GMStealthed)
                        client.Player.Stealth(true);
                }

                int coordsPerSec = 0;
                int jumpDetect = 0;
                int timediff = (int) (GameLoop.GameLoopTime - client.Player.LastPositionUpdatePacketReceivedTime);
                int distance = 0;

                if (timediff > 0)
                {
                    Point3D newPoint = new(realX, realY, realZ);
                    distance = newPoint.GetDistanceTo(new Point3D((int) client.Player.LastPositionUpdatePoint.X, (int) client.Player.LastPositionUpdatePoint.Y, (int) client.Player.LastPositionUpdatePoint.Z));
                    coordsPerSec = distance * 1000 / timediff;

                    if (distance < 100 && client.Player.LastPositionUpdatePoint.Z > 0)
                        jumpDetect = realZ - (int) client.Player.LastPositionUpdatePoint.Z;
                }

                if (distance > 0)
                    client.Player.LastPlayerActivityTime = GameLoop.GameLoopTime;

                client.Player.LastPositionUpdatePacketReceivedTime = GameLoop.GameLoopTime;
                client.Player.LastPositionUpdatePoint.X = realX;
                client.Player.LastPositionUpdatePoint.Y = realY;
                client.Player.LastPositionUpdatePoint.Z = realZ;
                int tolerance = ServerProperties.Properties.CPS_TOLERANCE;

                if (client.Player.Steed != null && client.Player.Steed.MaxSpeed > 0)
                    tolerance += client.Player.Steed.MaxSpeed;
                else if (client.Player.MaxSpeed > 0)
                    tolerance += client.Player.MaxSpeed;

                if (client.Player.IsJumping)
                {
                    coordsPerSec = 0;
                    jumpDetect = 0;
                    client.Player.IsJumping = false;
                }

                if (client.Player.IsAllowedToFly == false && (coordsPerSec > tolerance || jumpDetect > ServerProperties.Properties.JUMP_TOLERANCE))
                {
                    bool isHackDetected = true;

                    if (coordsPerSec > tolerance)
                    {
                        // check to see if CPS time tolerance is exceeded
                        int lastCPSTick = client.Player.TempProperties.GetProperty<int>(LASTCPSTICK);

                        if (environmentTick - lastCPSTick > ServerProperties.Properties.CPS_TIME_TOLERANCE)
                            isHackDetected = false;
                    }

                    if (isHackDetected)
                    {
                        StringBuilder builder = new();
                        builder.Append("MOVEHACK_DETECT");
                        builder.Append(": CharName=");
                        builder.Append(client.Player.Name);
                        builder.Append(" Account=");
                        builder.Append(client.Account.Name);
                        builder.Append(" IP=");
                        builder.Append(client.TcpEndpointAddress);
                        builder.Append(" CPS:=");
                        builder.Append(coordsPerSec);
                        builder.Append(" JT=");
                        builder.Append(jumpDetect);
                        ChatUtil.SendDebugMessage(client, builder.ToString());

                        if (client.Account.PrivLevel == 1)
                        {
                            GameServer.Instance.LogCheatAction(builder.ToString());

                            if (ServerProperties.Properties.ENABLE_MOVEDETECT)
                            {
                                if (ServerProperties.Properties.BAN_HACKERS && false) // banning disabled until this technique is proven accurate
                                {
                                    DbBans b = new()
                                    {
                                        Author = "SERVER",
                                        Ip = client.TcpEndpointAddress,
                                        Account = client.Account.Name,
                                        DateBan = DateTime.Now,
                                        Type = "B",
                                        Reason = string.Format("Autoban MOVEHACK:(CPS:{0}, JT:{1}) on player:{2}", coordsPerSec, jumpDetect, client.Player.Name)
                                    };

                                    GameServer.Database.AddObject(b);
                                    GameServer.Database.SaveObject(b);
                                    string message = "You have been auto kicked and banned due to movement hack detection!";

                                    for (int i = 0; i < 8; i++)
                                    {
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                    }

                                    client.Out.SendPlayerQuit(true);
                                    client.Player.SaveIntoDatabase();
                                    client.Player.Quit(true);
                                }
                                else
                                {
                                    string message = "You have been auto kicked due to movement hack detection!";

                                    for (int i = 0; i < 8; i++)
                                    {
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                        client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                    }

                                    client.Out.SendPlayerQuit(true);
                                    client.Player.SaveIntoDatabase();
                                    client.Player.Quit(true);
                                }

                                client.Disconnect();
                                return;
                            }
                        }
                    }

                    client.Player.TempProperties.SetProperty(LASTCPSTICK, environmentTick);
                }

                ushort headingflag = packet.ReadShort();
                ushort flyingflag = packet.ReadShort();
                ProcessActionFlags(client.Player, (ActionFlags) packet.ReadByte());

                client.Player.Heading = headingflag;
                client.Player.X = realX;
                client.Player.Y = realY;
                client.Player.Z = realZ;

                // update client zone information for waterlevel and diving
                if (zoneChange)
                    client.Out.SendPlayerPositionAndObjectID();

                // Begin ---------- New Area System -----------
                if (client.Player.CurrentRegion.Time > client.Player.AreaUpdateTick) // check if update is needed
                {
                    IList<IArea> oldAreas = client.Player.CurrentAreas;

                    // Because we may be in an instance we need to do the area check from the current region
                    // rather than relying on the zone which is in the skinned region.  - Tolakram

                    IList<IArea> newAreas = client.Player.CurrentRegion.GetAreasOfZone(newZone, client.Player);

                    // Check for left areas
                    if (oldAreas != null)
                    {
                        foreach (IArea area in oldAreas)
                        {
                            if (!newAreas.Contains(area))
                            {
                                area.OnPlayerLeave(client.Player);

                                //Check if leaving Border Keep areas so we can check RealmTimer
                                if (area is AbstractArea checkrvrarea && (checkrvrarea.Description.Equals("Castle Sauvage") ||
                                    checkrvrarea.Description.Equals("Snowdonia Fortress") ||
                                    checkrvrarea.Description.Equals("Svasud Faste") ||
                                    checkrvrarea.Description.Equals("Vindsaul Faste") ||
                                    checkrvrarea.Description.Equals("Druim Ligen") ||
                                    checkrvrarea.Description.Equals("Druim Cain")))
                                {
                                    RealmTimer.CheckRealmTimer(client.Player);
                                }
                            }
                        }
                    }

                    // Check for entered areas
                    foreach (IArea area in newAreas)
                    {
                        if (oldAreas == null || !oldAreas.Contains(area))
                            area.OnPlayerEnter(client.Player);
                    }

                    // set current areas to new one...
                    client.Player.CurrentAreas = newAreas;
                    client.Player.AreaUpdateTick = client.Player.CurrentRegion.Time + 750; // update every .75 seconds
                }
                // End ---------- New Area System -----------

                long SHlastTick = client.Player.TempProperties.GetProperty<long>(SHLASTUPDATETICK);
                int SHlastFly = client.Player.TempProperties.GetProperty<int>(SHLASTFLY);
                int SHlastStatus = client.Player.TempProperties.GetProperty<int>(SHLASTSTATUS);
                int SHcount = client.Player.TempProperties.GetProperty<int>(SHSPEEDCOUNTER);
                int status = (speedData & 0x1FF ^ speedData) >> 8;
                int fly = (flyingflag & 0x1FF ^ flyingflag) >> 8;

                if (client.Player.IsJumping)
                    SHcount = 0;

                if (SHlastTick != 0 && SHlastTick != environmentTick)
                {
                    if ((SHlastStatus == status || (status & 0x8) == 0) && ((fly & 0x80) != 0x80) && (SHlastFly == fly || (SHlastFly & 0x10) == (fly & 0x10) || !(((SHlastFly & 0x10) == 0x10) && ((fly & 0x10) == 0x0) && (flyingflag & 0x7FF) > 0)))
                    {
                        if ((environmentTick - SHlastTick) < 400)
                        {
                            SHcount++;

                            if (SHcount > 1 && client.Account.PrivLevel > 1)
                            {
                                //Apo: ?? no idea how to name the first parameter for language translation: 1: ??, 2: {detected} ?, 3: {count} ?
                                client.Out.SendMessage(string.Format("SH: ({0}) detected: {1}, count {2}", 500 / (environmentTick - SHlastTick), environmentTick - SHlastTick, SHcount), eChatType.CT_Staff, eChatLoc.CL_SystemWindow);
                            }

                            if (SHcount % 5 == 0)
                            {
                                StringBuilder builder = new();
                                builder.Append("TEST_SH_DETECT[");
                                builder.Append(SHcount);
                                builder.Append("] (");
                                builder.Append(environmentTick - SHlastTick);
                                builder.Append("): CharName=");
                                builder.Append(client.Player.Name);
                                builder.Append(" Account=");
                                builder.Append(client.Account.Name);
                                builder.Append(" IP=");
                                builder.Append(client.TcpEndpointAddress);
                                GameServer.Instance.LogCheatAction(builder.ToString());

                                if (client.Account.PrivLevel > 1)
                                {
                                    client.Out.SendMessage("SH: Logging SH cheat.", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);

                                    if (SHcount >= ServerProperties.Properties.SPEEDHACK_TOLERANCE)
                                        client.Out.SendMessage("SH: Player would have been banned!", eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                                }

                                if ((client.Account.PrivLevel == 1) && SHcount >= ServerProperties.Properties.SPEEDHACK_TOLERANCE)
                                {
                                    if (ServerProperties.Properties.BAN_HACKERS)
                                    {
                                        DbBans b = new()
                                        {
                                            Author = "SERVER",
                                            Ip = client.TcpEndpointAddress,
                                            Account = client.Account.Name,
                                            DateBan = DateTime.Now,
                                            Type = "B",
                                            Reason = string.Format("Autoban SH:({0},{1}) on player:{2}", SHcount, environmentTick - SHlastTick, client.Player.Name)
                                        };

                                        GameServer.Database.AddObject(b);
                                        GameServer.Database.SaveObject(b);
                                        string message = "You have been auto kicked and banned for speed hacking!";

                                        for (int i = 0; i < 8; i++)
                                        {
                                            client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                            client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                        }

                                        client.Out.SendPlayerQuit(true);
                                        client.Player.SaveIntoDatabase();
                                        client.Player.Quit(true);
                                    }
                                    else
                                    {
                                        string message = "You have been auto kicked for speed hacking!";

                                        for (int i = 0; i < 8; i++)
                                        {
                                            client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_SystemWindow);
                                            client.Out.SendMessage(message, eChatType.CT_Help, eChatLoc.CL_ChatWindow);
                                        }

                                        client.Out.SendPlayerQuit(true);
                                        client.Player.SaveIntoDatabase();
                                        client.Player.Quit(true);
                                    }

                                    client.Disconnect();
                                    return;
                                }
                            }
                        }
                        else
                            SHcount = 0;

                        SHlastTick = environmentTick;
                    }
                }
                else
                    SHlastTick = environmentTick;

                int state = (speedData >> 10) & 7;
                client.Player.IsClimbing = state == 7;
                client.Player.IsSwimming = state == 1;

                // debugFly on, but player not do /debug on (hack)
                if (state == 3 && !client.Player.TempProperties.GetProperty<bool>(GamePlayer.DEBUG_MODE_PROPERTY) && !client.Player.IsAllowedToFly)
                {
                    StringBuilder builder = new();
                    builder.Append("HACK_FLY");
                    builder.Append(": CharName=");
                    builder.Append(client.Player.Name);
                    builder.Append(" Account=");
                    builder.Append(client.Account.Name);
                    builder.Append(" IP=");
                    builder.Append(client.TcpEndpointAddress);

                    GameServer.Instance.LogCheatAction(builder.ToString());
                    {
                        if (ServerProperties.Properties.BAN_HACKERS)
                        {
                            DbBans b = new()
                            {
                                Author = "SERVER",
                                Ip = client.TcpEndpointAddress,
                                Account = client.Account.Name,
                                DateBan = DateTime.Now,
                                Type = "B",
                                Reason = string.Format("Autoban flying hack: on player:{0}", client.Player.Name)
                            };

                            GameServer.Database.AddObject(b);
                            GameServer.Database.SaveObject(b);
                        }

                        string message = "Client Hack Detected!";

                        for (int i = 0; i < 6; i++)
                        {
                            client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            client.Out.SendMessage(message, eChatType.CT_System, eChatLoc.CL_ChatWindow);
                        }

                        client.Out.SendPlayerQuit(true);
                        client.Disconnect();
                        return;
                    }
                }

                SHlastFly = fly;
                SHlastStatus = status;
                client.Player.TempProperties.SetProperty(SHLASTUPDATETICK, SHlastTick);
                client.Player.TempProperties.SetProperty(SHLASTFLY, SHlastFly);
                client.Player.TempProperties.SetProperty(SHLASTSTATUS, SHlastStatus);
                client.Player.TempProperties.SetProperty(SHSPEEDCOUNTER, SHcount);

                GameLocation[] locations = client.Player.LastUniqueLocations;
                GameLocation loc = locations[0];

                if (loc.X != realX || loc.Y != realY || loc.Z != realZ || loc.RegionID != client.Player.CurrentRegionID)
                {
                    loc = locations[^1];
                    Array.Copy(locations, 0, locations, 1, locations.Length - 1);
                    locations[0] = loc;
                    loc.X = realX;
                    loc.Y = realY;
                    loc.Z = realZ;
                    loc.Heading = client.Player.Heading;
                    loc.RegionID = client.Player.CurrentRegionID;
                }

                //**************//
                //FALLING DAMAGE//
                //**************//
                int fallSpeed;

                if (GameServer.ServerRules.CanTakeFallDamage(client.Player) && !client.Player.IsSwimming)
                {
                    int maxLastZ = client.Player.MaxLastZ;

                    /* Are we on the ground? */
                    if ((flyingflag >> 15) != 0)
                    {
                        int safeFallLevel = client.Player.GetAbilityLevel(Abilities.SafeFall);
                        fallSpeed = (flyingflag & 0xFFF) - 100 * safeFallLevel; // 0x7FF fall speed and 0x800 bit = fall speed overcaped
                        client.Player.FallSpeed = (short) fallSpeed;
                        int fallMinSpeed = 400;
                        int fallDivide = 6;

                        if (client.Version >= GameClient.eClientVersion.Version188)
                        {
                            fallMinSpeed = 500;
                            fallDivide = 15;
                        }

                        int fallPercent = Math.Min(99, (fallSpeed - (fallMinSpeed + 1)) / fallDivide);

                        if (fallSpeed > fallMinSpeed)
                            client.Player.CalcFallDamage(fallPercent);

                        client.Player.MaxLastZ = client.Player.Z;
                    }

                    else
                    {
                        // always set Z if on the ground
                        if (flyingflag == 0)
                            client.Player.MaxLastZ = client.Player.Z;
                        // set Z if in air and higher than old Z
                        else if (maxLastZ < client.Player.Z)
                            client.Player.MaxLastZ = client.Player.Z;
                    }
                }
                //**************//

                //Riding is set here!
                if (client.Player.Steed != null && client.Player.Steed.ObjectState is GameObject.eObjectState.Active)
                    client.Player.Heading = client.Player.Steed.Heading;

                if ((eCharacterClass) client.Player.CharacterClass.ID is eCharacterClass.Warlock)
                {
                    //Send Chamber effect
                    client.Player.Out.SendWarlockChamberEffect(client.Player);
                }

                //handle closing of windows
                //trade window
                if (client.Player.TradeWindow != null)
                {
                    if (client.Player.TradeWindow.Partner != null)
                    {
                        if (!client.Player.IsWithinRadius(client.Player.TradeWindow.Partner, WorldMgr.GIVE_ITEM_DISTANCE))
                            client.Player.TradeWindow.CloseTrade();
                    }
                }
            }
        }

        public static void BroadcastPosition(GameClient client)
        {
            if (client.Version >= GameClient.eClientVersion.Version1124)
                BroadcastPositionSince1124(client);
            else
                BroadcastPositionPre1124(client); // Likely outdated and bugged.

            static void BroadcastPositionSince1124(GameClient client)
            {
                GamePlayer player = client.Player;
                ActionFlags actionFlags = GetActionFlagsOut(player);
                StateFlags stateFlags = player.StateFlags;
                byte healthByte = GetHealthByte(player);
                ushort steedSeatPosition = GetSeatPosition(player);
                ushort heading;

                if (player.Steed != null && player.Steed.ObjectState is GameObject.eObjectState.Active)
                    heading = (ushort) client.Player.Steed.ObjectID;
                else
                    heading = player.RawHeading;

                GSUDPPacketOut outPak1127 = null;
                GSUDPPacketOut outPak1124 = null;
                GSUDPPacketOut outPak1112 = null;
                GSUDPPacketOut outPak190 = null;

                foreach (GamePlayer otherPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (otherPlayer == player)
                        continue;

                    if ((player.InHouse || otherPlayer.InHouse) && otherPlayer.CurrentHouse != player.CurrentHouse)
                        continue;

                    if (otherPlayer.CanDetect(player))
                    {
                        if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1127)
                        {
                            outPak1127 ??= CreateOutPak1127();
                            otherPlayer.Out.SendUDP(outPak1127);
                        }
                        else if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1124)
                        {
                            outPak1124 ??= CreateOutPak1124();
                            otherPlayer.Out.SendUDP(outPak1124);
                        }
                        else if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1112)
                        {
                            outPak1112 ??= CreateOutPak1112();
                            otherPlayer.Out.SendUDP(outPak1112);
                        }
                        else
                        {
                            outPak190 ??= CreateOutPak190();
                            otherPlayer.Out.SendUDP(outPak190);
                        }
                    }
                    else
                        otherPlayer.Out.SendObjectDelete(player); // Remove the stealthed player from view.
                }

                GSUDPPacketOut CreateOutPak1127()
                {
                    GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                    outPak.WriteFloatLowEndian(player.X);
                    outPak.WriteFloatLowEndian(player.Y);
                    outPak.WriteFloatLowEndian(player.Z);
                    outPak.WriteFloatLowEndian(player.CurrentSpeed);
                    outPak.WriteFloatLowEndian(player.FallSpeed);
                    outPak.WriteShort((ushort) client.SessionID);
                    outPak.WriteShort((ushort) player.ObjectID);
                    outPak.WriteShort(player.CurrentZone.ID);
                    outPak.WriteByte((byte) stateFlags);
                    outPak.WriteByte(0);
                    outPak.WriteShort(steedSeatPosition); // Fall damage flag coming in, steed seat position going out.
                    outPak.WriteShort(heading);
                    outPak.WriteByte((byte) actionFlags);
                    outPak.WriteByte((byte) (player.RPFlag ? 1 : 0));
                    outPak.WriteByte(0);
                    outPak.WriteByte(healthByte);
                    outPak.WriteByte(player.ManaPercent);
                    outPak.WriteByte(player.EndurancePercent);
                    outPak.WriteShort(0);
                    return outPak;
                }

                GSUDPPacketOut CreateOutPak1124()
                {
                    GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                    outPak.WriteFloatLowEndian(player.X);
                    outPak.WriteFloatLowEndian(player.Y);
                    outPak.WriteFloatLowEndian(player.Z);
                    outPak.WriteFloatLowEndian(player.CurrentSpeed);
                    outPak.WriteFloatLowEndian(player.FallSpeed);
                    outPak.WriteShort((ushort) client.SessionID);
                    outPak.WriteShort(player.CurrentZone.ID);
                    outPak.WriteByte((byte) stateFlags);
                    outPak.WriteByte(0);
                    outPak.WriteShort(steedSeatPosition); // Fall damage flag coming in, steed seat position going out.
                    outPak.WriteShort(heading);
                    outPak.WriteByte((byte) actionFlags);
                    outPak.WriteByte((byte) (player.RPFlag ? 1 : 0));
                    outPak.WriteByte(0);
                    outPak.WriteByte(healthByte);
                    outPak.WriteByte(player.ManaPercent);
                    outPak.WriteByte(player.EndurancePercent);
                    return outPak;
                }

                GSUDPPacketOut CreateOutPak1112()
                {
                    GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                    outPak.WriteShort((ushort) client.SessionID);
                    outPak.WriteShort((ushort) (player.CurrentSpeed & 0x1FF));
                    outPak.WriteShort((ushort) player.Z);
                    ushort xOffset = (ushort) (player.X - (player.CurrentZone?.XOffset ?? 0));
                    outPak.WriteShort(xOffset);
                    ushort yOffset = (ushort) (player.Y - (player.CurrentZone?.YOffset ?? 0));
                    outPak.WriteShort(yOffset);
                    outPak.WriteShort(player.CurrentZone.ID);
                    outPak.WriteShort(heading);
                    outPak.WriteShort(steedSeatPosition);
                    outPak.WriteByte((byte) actionFlags);
                    outPak.WriteByte(healthByte);
                    outPak.WriteByte(player.ManaPercent);
                    outPak.WriteByte(player.EndurancePercent);
                    outPak.WriteByte((byte) (player.RPFlag ? 1 : 0));
                    outPak.WriteByte(0);
                    return outPak;
                }

                GSUDPPacketOut CreateOutPak190()
                {
                    GSUDPPacketOut outPak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                    outPak.WriteShort((ushort) client.SessionID);
                    outPak.WriteShort((ushort) (player.CurrentSpeed & 0x1FF));
                    outPak.WriteShort((ushort) player.Z);
                    ushort xOffset = (ushort) (player.X - (player.CurrentZone?.XOffset ?? 0));
                    outPak.WriteShort(xOffset);
                    ushort yOffset = (ushort) (player.Y - (player.CurrentZone?.YOffset ?? 0));
                    outPak.WriteShort(yOffset);
                    outPak.WriteShort(player.CurrentZone.ID);
                    outPak.WriteShort(heading);
                    outPak.WriteShort(steedSeatPosition);
                    outPak.WriteByte((byte) actionFlags);
                    outPak.WriteByte(healthByte);
                    outPak.WriteByte(player.ManaPercent);
                    outPak.WriteByte(player.EndurancePercent);
                    outPak.FillString(player.CharacterClass.Name, 32);
                    outPak.WriteByte((byte) (player.RPFlag ? 1 : 0));
                    outPak.WriteByte(0);
                    return outPak;
                }
            }

            static void BroadcastPositionPre1124(GameClient client)
            {
                GamePlayer player = client.Player;
                GSUDPPacketOut outpak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                byte healthByte = GetHealthByte(player);
                ushort seatPosition = GetSeatPosition(player);
                outpak.WriteShort((ushort) client.SessionID);

                if (player.Steed != null && player.Steed.ObjectState is GameObject.eObjectState.Active)
                    outpak.WriteShort(0x1800);
                else
                {
                    int rSpeed = player.IsIncapacitated ? 0 : player.CurrentSpeed;
                    ushort content;

                    if (rSpeed < 0)
                        content = (ushort) ((rSpeed < -511 ? 511 : -rSpeed) + 0x200);
                    else
                        content = (ushort) (rSpeed > 511 ? 511 : rSpeed);

                    if (!player.IsAlive)
                        content |= 5 << 10;
                    else
                    {
                        ushort pState = 0;

                        if (player.IsSwimming)
                            pState = 1;
                        if (player.IsClimbing)
                            pState = 7;
                        if (player.IsSitting)
                            pState = 4;
                        if (player.IsStrafing)
                            pState |= 8;

                        content |= (ushort) (pState << 10);
                    }

                    outpak.WriteShort(content);
                }

                outpak.WriteShort((ushort) player.Z);
                outpak.WriteShort((ushort) (player.X - player.CurrentZone.XOffset));
                outpak.WriteShort((ushort) (player.Y - player.CurrentZone.YOffset));
                outpak.WriteShort(player.CurrentZone.ZoneSkinID);

                // Copy Heading && Falling or Write Steed
                if (player.Steed != null && player.Steed.ObjectState is GameObject.eObjectState.Active)
                {
                    outpak.WriteShort((ushort) player.Steed.ObjectID);
                    outpak.WriteShort(seatPosition);
                }
                else
                {
                    // Set Player always on ground, this is an "anti lag" packet
                    ushort contentHead = (ushort) (player.Heading + (true ? 0x1000 : 0));
                    outpak.WriteShort(contentHead);
                    outpak.WriteShort(0); // No Fall Speed.
                }

                // Write Flags
                byte flagcontent = 0;

                if (player.IsWireframe)
                    flagcontent |= 0x01;
                if (player.IsStealthed)
                    flagcontent |= 0x02;
                if (player.IsDiving)
                    flagcontent |= 0x04;
                if (player.IsTorchLighted)
                    flagcontent |= 0x80;

                outpak.WriteByte(flagcontent);
                outpak.WriteByte(healthByte);
                outpak.WriteByte(player.ManaPercent);
                outpak.WriteByte(player.EndurancePercent);
                byte[] outpakArr = outpak.ToArray();

                GSUDPPacketOut outpak190 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak190.Write(outpakArr, 5, outpakArr.Length - 5);
                outpak190.FillString(player.CharacterClass.Name, 32);
                outpak190.WriteByte((byte)(player.RPFlag ? 1 : 0)); // roleplay flag, if == 1, show name (RP) with gray color
                outpak190.WriteByte(0); // send last byte for 190+ packets

                GSUDPPacketOut outpak1112 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak1112.Write(outpakArr, 5, outpakArr.Length - 5);
                outpak1112.WriteByte((byte) (player.RPFlag ? 1 : 0));
                outpak1112.WriteByte(0); //outpak1112.WriteByte((con168.Length == 22) ? con168[21] : (byte)0);

                GSUDPPacketOut outpak1124 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak1124.WriteFloatLowEndian(player.X);
                outpak1124.WriteFloatLowEndian(player.Y);
                outpak1124.WriteFloatLowEndian(player.Z);
                outpak1124.WriteFloatLowEndian(player.CurrentSpeed);
                outpak1124.WriteFloatLowEndian(player.FallSpeed);
                outpak1124.WriteShort((ushort) client.SessionID);
                outpak1124.WriteShort(player.CurrentZone.ID);
                outpak1124.WriteShort(0); // Missing.
                outpak1124.WriteShort(seatPosition); // fall damage flag coming in, steed seat position going out
                outpak1124.WriteShort(player.RawHeading);
                outpak1124.WriteByte((byte) GetActionFlagsOut(player));
                outpak1124.WriteByte((byte) (player.RPFlag ? 1 : 0));
                outpak1124.WriteByte(0);
                outpak1124.WriteByte(healthByte);
                outpak1124.WriteByte(player.ManaPercent);
                outpak1124.WriteByte(player.EndurancePercent);

                foreach (GamePlayer otherPlayer in player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (otherPlayer == player)
                        continue;

                    if ((player.InHouse || otherPlayer.InHouse) && otherPlayer.CurrentHouse != player.CurrentHouse)
                        continue;

                    if (player.MinotaurRelic != null)
                    {
                        MinotaurRelic relic = player.MinotaurRelic;
                        if (!relic.Playerlist.Contains(otherPlayer) && otherPlayer != player)
                        {
                            relic.Playerlist.Add(otherPlayer);
                            otherPlayer.Out.SendMinotaurRelicWindow(player, player.MinotaurRelic.Effect, true);
                        }
                    }

                    if (otherPlayer.CanDetect(player))
                    {
                        //forward the position packet like normal!
                        if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1124)
                            otherPlayer.Out.SendUDP(outpak1124);
                        else if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version1112)
                            otherPlayer.Out.SendUDP(outpak1112);
                        else if (otherPlayer.Client.Version >= GameClient.eClientVersion.Version190)
                            otherPlayer.Out.SendUDP(outpak190);
                    }
                    else
                        otherPlayer.Out.SendObjectDelete(player); //remove the stealthed player from view
                }
            }
        }

        public static byte GetHealthByte(GamePlayer player)
        {
            return (byte) (player.HealthPercent + (player.attackComponent.AttackState ? 0x80 : 0));
        }

        public static ushort GetSeatPosition(GamePlayer player)
        {
            return (ushort) (player.Steed is null ? 0 : player.Steed.RiderSlot(player));
        }

        public static bool ProcessStateFlags(GamePlayer player, StateFlags stateFlags)
        {
            player.IsStrafing = (stateFlags & StateFlags.STRAFING_ANY) != 0;
            player.IsClimbing = (stateFlags & StateFlags.CLIMBING) is StateFlags.CLIMBING;

            // CLIMBING combines SITTING, JUMPING, SWIMMING and is always allowed.
            // Other combinations can cause issues.
            if (!player.IsClimbing)
            {
                // This turns the player invisible if it isn't riding.
                if ((stateFlags & StateFlags.RIDING) is StateFlags.RIDING && !player.IsRiding)
                {
                    if (ServerProperties.Properties.BAN_HACKERS)
                    {
                        player.Client.BanAccount($"Autoban forged position update packet ({nameof(stateFlags)}: {StateFlags.RIDING})");
                        player.Out.SendPlayerQuit(true);
                        player.Client.Disconnect();
                        return false;
                    }

                    stateFlags &= ~StateFlags.RIDING;
                }

                // Sitting and swimming (death animation). Don't allow players to play dead.
                // Clients that just got resurrected but aren't aware yet also send this.
                if ((stateFlags & StateFlags.DEAD) is StateFlags.DEAD && player.HealthPercent > 0)
                    stateFlags &= ~StateFlags.DEAD;

                // If the client has flying enabled but the debug option wasn't enabled.
                if ((stateFlags & StateFlags.FLYING) is StateFlags.FLYING && !player.TempProperties.GetProperty<bool>(GamePlayer.DEBUG_MODE_PROPERTY) && !player.IsAllowedToFly)
                {
                    if (ServerProperties.Properties.BAN_HACKERS)
                    {
                        player.Client.BanAccount($"Autoban forged position update packet ({nameof(stateFlags)}: {StateFlags.FLYING})");
                        player.Out.SendPlayerQuit(true);
                        player.Client.Disconnect();
                        return false;
                    }

                    stateFlags &= ~StateFlags.FLYING;
                }
            }

            player.StateFlags = stateFlags;
            return true;
        }

        public static void ProcessActionFlags(GamePlayer player, ActionFlags actionFlags)
        {
            // Don't trust the client to set it to true. We rely on that to detect move hacks.
            if ((actionFlags & ActionFlags.TELEPORT) == 0)
                player.IsJumping = false;

            player.TargetInView = (actionFlags & ActionFlags.TARGET_IN_VIEW) is ActionFlags.TARGET_IN_VIEW;
            player.GroundTargetInView = (actionFlags & ActionFlags.GROUNT_TARGET_IN_VIEW) != 0;
            player.IsTorchLighted = (actionFlags & ActionFlags.TORCH) != 0;
            player.IsDiving = (actionFlags & ActionFlags.DIVING_IN__STEALTHED_OUT) != 0;
            player.ActionFlags = actionFlags;
        }

        public static ActionFlags GetActionFlagsOut(GamePlayer player)
        {
            ActionFlags actionFlags = player.ActionFlags;

            if (player.IsStealthed)
                actionFlags |= ActionFlags.DIVING_IN__STEALTHED_OUT;
            else
                actionFlags &= ~ActionFlags.DIVING_IN__STEALTHED_OUT;

            if (player.IsDiving)
                actionFlags |= ActionFlags.PET_IN_VIEW_IN__DIVING_OUT;
            else
                actionFlags &= ~ActionFlags.PET_IN_VIEW_IN__DIVING_OUT;

            return actionFlags;
        }

        [Flags]
        public enum StateFlags : byte
        {
            STRAFING_RIGHT =      1 << 7,
            STRAFING_LEFT =       1 << 6,
            STRAFING_FULL_SPEED = 1 << 5,
            STRAFING_ANY =        STRAFING_RIGHT | STRAFING_LEFT | STRAFING_FULL_SPEED,
            SITTING =             1 << 4,
            JUMPING =             1 << 3,
            SWIMMING =            1 << 2,
            DEAD =                SITTING | SWIMMING,
            RIDING =              SITTING | JUMPING,
            FLYING =              JUMPING | SWIMMING,
            CLIMBING =            SITTING | JUMPING | SWIMMING
        }

        [Flags]
        public enum ActionFlags : byte
        {
            TORCH =                         1 << 7,
            TELEPORT =                      1 << 6,
            TARGET_IN_VIEW =                (1 << 5) | (1 << 4),
            GROUNT_TARGET_IN_VIEW =         1 << 3,
            PET_IN_VIEW_IN__DIVING_OUT =    1 << 2,
            DIVING_IN__STEALTHED_OUT =      1 << 1,
            WIREFRAME =                     1
        }
    }
}
