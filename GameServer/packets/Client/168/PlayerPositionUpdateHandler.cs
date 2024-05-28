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
            if (!client.Player.OnUpdatePosition())
                return;

            //Tiv: in very rare cases client send 0xA9 packet before sending S<=C 0xE8 player world initialize
            if ((client.Player.ObjectState != GameObject.eObjectState.Active) || (client.ClientState != GameClient.eClientState.Playing))
                return;

            // Don't allow movement if the player isn't close to the NPC they're supposed to be riding.
            // Instead, teleport them to it and send an update packet (the client may then ask for a create packet).
            if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
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

            HandlePacketInternal(client, packet);
            client.LastPositionUpdatePacketReceived = packet;
        }

        public static void BroadcastLastReceivedPacket(GameClient client)
        {
            GSPacketIn packet = client.LastPositionUpdatePacketReceived;

            if (packet == null)
                return;

            packet.Position = 0;
            HandlePacketInternal(client, packet);
        }

        private static void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            if (client.Version >= GameClient.eClientVersion.Version1124)
                HandlePacketSince1124(client, packet);
            else
                HandlePacketPre1124(client, packet);
        }

        private static void HandlePacketSince1124(GameClient client, GSPacketIn packet)
        {
            //Tiv: in very rare cases client send 0xA9 packet before sending S<=C 0xE8 player world initialize
            if (client.Player.ObjectState != GameObject.eObjectState.Active || client.ClientState is not GameClient.eClientState.Playing and not GameClient.eClientState.Linkdead)
                return;

            long environmentTick = GameLoop.GameLoopTime;

            float x = packet.ReadFloatLowEndian();
            float y = packet.ReadFloatLowEndian();
            float z = packet.ReadFloatLowEndian();
            float speed = packet.ReadFloatLowEndian();
            float zSpeed = packet.ReadFloatLowEndian();
            ushort sessionId = packet.ReadShort();

            if (client.Version >= GameClient.eClientVersion.Version1127)
                packet.Skip(2); // object ID

            ushort zoneId = packet.ReadShort();
            State state = (State) packet.ReadByte();
            packet.Skip(1); // Unknown.
            ushort fallingDamage = packet.ReadShort();
            ushort heading = packet.ReadShort();
            Action action = (Action) packet.ReadByte();
            packet.Skip(2); // unknown bytes x2
            packet.Skip(1); // Health.
            // two trailing bytes, no data + 2 more for 1.127+

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

            client.Player.IsStrafing = (state & State.STRAFING_ANY) != 0;
            client.Player.IsClimbing = (state & State.CLIMBING) is State.CLIMBING;

            // CLIMBING combines SITTING, JUMPING, SWIMMING and is always allowed.
            if (!client.Player.IsClimbing)
            {
                // This turns the player invisible if it isn't riding.
                if ((state & State.RIDING) is State.RIDING && !client.Player.IsRiding)
                {
                    if (ServerProperties.Properties.BAN_HACKERS)
                    {
                        client.BanAccount($"Autoban forged position update packet ({nameof(state)}: {State.SITTING | State.JUMPING})");
                        client.Out.SendPlayerQuit(true);
                        client.Disconnect();
                        return;
                    }

                    state &= ~State.RIDING;
                }

                // Sitting and swimming (death animation). Don't allow players to play dead.
                // Clients that just got resurrected but aren't aware yet also send this.
                if ((state & State.DEAD) is State.DEAD && client.Player.HealthPercent > 0)
                    state &= ~State.DEAD;

                // If the client has flying enabled but the debug option wasn't enabled.
                if ((state & State.FLYING) is State.FLYING && !client.Player.TempProperties.GetProperty(GamePlayer.DEBUG_MODE_PROPERTY, false) && !client.Player.IsAllowedToFly)
                {
                    if (ServerProperties.Properties.BAN_HACKERS)
                    {
                        client.BanAccount($"Autoban forged position update packet ({nameof(state)}: {State.FLYING})");
                        client.Out.SendPlayerQuit(true);
                        client.Disconnect();
                        return;
                    }

                    state &= ~State.FLYING;
                }

                client.Player.IsSwimming = (state & State.SWIMMING) != 0;
            }

            Zone newZone = WorldMgr.GetZone(zoneId);

            if (newZone == null)
            {
                if (!client.Player.TempProperties.GetProperty("isbeingbanned", false))
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
            long timediff = GameLoop.GameLoopTime - client.Player.LastPositionUpdateTime;
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

            client.Player.LastPositionUpdateTime = GameLoop.GameLoopTime;
            client.Player.LastPositionUpdatePoint.X = x;
            client.Player.LastPositionUpdatePoint.Y = y;
            client.Player.LastPositionUpdatePoint.Z = z;
            int tolerance = ServerProperties.Properties.CPS_TOLERANCE;

            if (client.Player.Steed != null && client.Player.Steed.MaxSpeed > 0)
                tolerance += client.Player.Steed.MaxSpeed;
            else if (client.Player.MaxSpeed > 0)
                tolerance += client.Player.MaxSpeed;

            // Don't trust the client to set it to true. We rely on that to detect move hacks.
            if ((action & Action.TELEPORT) == 0)
                client.Player.IsJumping = false;

            client.Player.TargetInView = (action & Action.TARGET_IN_VIEW) is Action.TARGET_IN_VIEW;
            client.Player.GroundTargetInView = (action & Action.GROUNT_TARGET_IN_VIEW) != 0;
            client.Player.IsTorchLighted = (action & Action.TORCH) != 0;
            // patch 0069 player diving is 0x02, but will broadcast to other players as 0x04
            // if player has a pet summoned, player action is sent by client as 0x04, but sending to other players this is skipped
            client.Player.IsDiving = (action & Action.DIVING) != 0;

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
                    int lastCPSTick = client.Player.TempProperties.GetProperty(LASTCPSTICK, 0);

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

            client.Player.X = (int) x;
            client.Player.Y = (int) y;
            client.Player.Z = (int) z;
            client.Player.Heading = (ushort) (heading & 0xFFF);

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
                client.Player.AreaUpdateTick = client.Player.CurrentRegion.Time + 2000; // update every 2 seconds
            }
            // End ---------- New Area System -----------

            lock (client.Player.LastUniqueLocations)
            {
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
            }

            //FALLING DAMAGE
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
                        {
                            if (client.Player.CharacterClass.ID != (int) eCharacterClass.Necromancer || !client.Player.IsShade)
                                client.Player.CalcFallDamage(fallPercent);
                        }

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

            ushort steedSeatPosition = 0;

            if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
            {
                client.Player.Heading = client.Player.Steed.Heading;
                heading = (ushort) client.Player.Steed.ObjectID;
                steedSeatPosition = (ushort)client.Player.Steed.RiderSlot(client.Player);
            }

            BroadcastPositionSince1124();

            //handle closing of windows
            //trade window
            if (client.Player.TradeWindow?.Partner != null && !client.Player.IsWithinRadius(client.Player.TradeWindow.Partner, WorldMgr.GIVE_ITEM_DISTANCE))
                client.Player.TradeWindow.CloseTrade();

            void BroadcastPositionSince1124()
            {
                GSUDPPacketOut outpak1124 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                //patch 0069 test to fix player swim out byte flag
                byte playerOutAction = 0x00;
                if (client.Player.IsDiving)
                    playerOutAction |= 0x04;
                if (client.Player.TargetInView)
                    playerOutAction |= 0x30;
                if (client.Player.GroundTargetInView)
                    playerOutAction |= 0x08;
                if (client.Player.IsTorchLighted)
                    playerOutAction |= 0x80;
                if (client.Player.IsStealthed)
                    playerOutAction |= 0x02;

                outpak1124.WriteFloatLowEndian(x);
                outpak1124.WriteFloatLowEndian(y);
                outpak1124.WriteFloatLowEndian(z);
                outpak1124.WriteFloatLowEndian(speed);
                outpak1124.WriteFloatLowEndian(zSpeed);
                outpak1124.WriteShort(sessionId);
                outpak1124.WriteShort(zoneId);
                outpak1124.WriteByte((byte) state);
                outpak1124.WriteByte(0);
                outpak1124.WriteShort(steedSeatPosition); // fall damage flag coming in, steed seat position going out
                outpak1124.WriteShort(heading);
                outpak1124.WriteByte(playerOutAction);
                outpak1124.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak1124.WriteByte(0);
                outpak1124.WriteByte((byte) (client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
                outpak1124.WriteByte(client.Player.ManaPercent);
                outpak1124.WriteByte(client.Player.EndurancePercent);

                GSUDPPacketOut outpak1127 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak1127.Write(outpak1124.GetBuffer(), 5, 22); // from position X to sessionID
                outpak1127.WriteShort((ushort) client.Player.ObjectID);
                outpak1127.WriteShort(zoneId);
                outpak1127.WriteByte((byte) state);
                outpak1127.WriteByte(0);
                outpak1127.WriteShort(steedSeatPosition); // fall damage flag coming in, steed seat position going out
                outpak1127.WriteShort(heading);
                outpak1127.WriteByte(playerOutAction);
                outpak1127.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak1127.WriteByte(0);
                outpak1127.WriteByte((byte) (client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
                outpak1127.WriteByte(client.Player.ManaPercent);
                outpak1127.WriteByte(client.Player.EndurancePercent);
                outpak1127.WriteShort(0);

                GSUDPPacketOut outpak190 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak190.WriteShort((ushort) client.SessionID);
                outpak190.WriteShort((ushort) (client.Player.CurrentSpeed & 0x1FF));
                outpak190.WriteShort((ushort) z);
                ushort xoff = (ushort) (x - (client.Player.CurrentZone?.XOffset ?? 0));
                outpak190.WriteShort(xoff);
                ushort yoff = (ushort) (y - (client.Player.CurrentZone?.YOffset ?? 0));
                outpak190.WriteShort(yoff);
                outpak190.WriteShort(zoneId);
                outpak190.WriteShort(heading);
                outpak190.WriteShort(steedSeatPosition);
                outpak190.WriteByte((byte) action);
                outpak190.WriteByte((byte) (client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
                outpak190.WriteByte(client.Player.ManaPercent);
                outpak190.WriteByte(client.Player.EndurancePercent);

                GSUDPPacketOut outpak1112 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak1112.Write(outpak190.GetBuffer(), 5, (int) outpak190.Length - 5);
                outpak1112.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak1112.WriteByte(0);

                outpak190.FillString(client.Player.CharacterClass.Name, 32);
                outpak190.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak190.WriteByte(0);

                foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null || player == client.Player)
                        continue;

                    if ((client.Player.InHouse || player.InHouse) && player.CurrentHouse != client.Player.CurrentHouse)
                        continue;

                    if (!client.Player.IsStealthed || player.CanDetect(client.Player))
                    {
                        if (player.Client.Version >= GameClient.eClientVersion.Version1127)
                            player.Out.SendUDP(outpak1127);
                        else if (player.Client.Version >= GameClient.eClientVersion.Version1124)
                            player.Out.SendUDP(outpak1124);
                        else if (player.Client.Version >= GameClient.eClientVersion.Version1112)
                            player.Out.SendUDP(outpak1112);
                        else
                            player.Out.SendUDP(outpak190);
                    }
                    else
                        player.Out.SendObjectDelete(client.Player); //remove the stealthed player from view
                }
            }
        }
        private static void HandlePacketPre1124(GameClient client, GSPacketIn packet)
        {
            long environmentTick = GameLoop.GameLoopTime;
            int oldSpeed = client.Player.CurrentSpeed;
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

                if (!client.Player.TempProperties.GetProperty("isbeingbanned",false))
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
            int timediff = (int) (GameLoop.GameLoopTime - client.Player.LastPositionUpdateTime);
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

            client.Player.LastPositionUpdateTime = GameLoop.GameLoopTime;
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
                    int lastCPSTick = client.Player.TempProperties.GetProperty(LASTCPSTICK, 0);

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
            byte flags = (byte) packet.ReadByte();

            client.Player.Heading = (ushort) (headingflag & 0xFFF);
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

            client.Player.TargetInView = (flags & 0x10) != 0;
            client.Player.GroundTargetInView = (flags & 0x08) != 0;
            client.Player.IsTorchLighted = (flags & 0x80) != 0;
            client.Player.IsDiving = (flags & 0x02) != 0x00;
            //7  6  5  4  3  2  1 0
            //15 14 13 12 11 10 9 8
            //                1 1

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
            if (state == 3 && !client.Player.TempProperties.GetProperty(GamePlayer.DEBUG_MODE_PROPERTY, false) && !client.Player.IsAllowedToFly)
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

            lock (client.Player.LastUniqueLocations)
            {
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
            }

            //**************//
            //FALLING DAMAGE//
            //**************//
            double fallDamage = 0;
            int fallSpeed = 0;

            if (GameServer.ServerRules.CanTakeFallDamage(client.Player) && !client.Player.IsSwimming)
            {
                int maxLastZ = client.Player.MaxLastZ;

                /* Are we on the ground? */
                if ((flyingflag >> 15) != 0)
                {
                    int safeFallLevel = client.Player.GetAbilityLevel(Abilities.SafeFall);
                    fallSpeed = (flyingflag & 0xFFF) - 100 * safeFallLevel; // 0x7FF fall speed and 0x800 bit = fall speed overcaped
                    int fallMinSpeed = 400;
                    int fallDivide = 6;

                    if (client.Version >= GameClient.eClientVersion.Version188)
                    {
                        fallMinSpeed = 500;
                        fallDivide = 15;
                    }

                    int fallPercent = Math.Min(99, (fallSpeed - (fallMinSpeed + 1)) / fallDivide);

                    if (fallSpeed > fallMinSpeed)
                    {
                        // client.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "PlayerPositionUpdateHandler.FallingDamage"),
                        // eChatType.CT_Damaged, eChatLoc.CL_SystemWindow);
                        fallDamage = client.Player.CalcFallDamage(fallPercent);
                    }

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
            if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
                client.Player.Heading = client.Player.Steed.Heading;

            BroadcastPositionPre1124();

            if (client.Player.CharacterClass.ID == (int)eCharacterClass.Warlock)
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

            void BroadcastPositionPre1124()
            {
                GSUDPPacketOut outpak = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak.WriteShort((ushort)client.SessionID);

                if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
                    outpak.WriteShort(0x1800);
                else
                {
                    int rSpeed = client.Player.IsIncapacitated ? 0 : client.Player.CurrentSpeed;
                    ushort content;

                    if (rSpeed < 0)
                        content = (ushort) ((rSpeed < -511 ? 511 : -rSpeed) + 0x200);
                    else
                        content = (ushort) (rSpeed > 511 ? 511 : rSpeed);

                    if (!client.Player.IsAlive)
                        content |= 5 << 10;
                    else
                    {
                        ushort pState = 0;

                        if (client.Player.IsSwimming)
                            pState = 1;
                        if (client.Player.IsClimbing)
                            pState = 7;
                        if (client.Player.IsSitting)
                            pState = 4;
                        if (client.Player.IsStrafing)
                            pState |= 8;

                        content |= (ushort) (pState << 10);
                    }

                    outpak.WriteShort(content);
                }

                outpak.WriteShort((ushort) client.Player.Z);
                outpak.WriteShort((ushort) (client.Player.X - client.Player.CurrentZone.XOffset));
                outpak.WriteShort((ushort) (client.Player.Y - client.Player.CurrentZone.YOffset));
                outpak.WriteShort(client.Player.CurrentZone.ZoneSkinID);

                // Copy Heading && Falling or Write Steed
                if (client.Player.Steed != null && client.Player.Steed.ObjectState == GameObject.eObjectState.Active)
                {
                    outpak.WriteShort((ushort) client.Player.Steed.ObjectID);
                    outpak.WriteShort((ushort) client.Player.Steed.RiderSlot(client.Player));
                }
                else
                {
                    // Set Player always on ground, this is an "anti lag" packet
                    ushort contentHead = (ushort) (client.Player.Heading + (true ? 0x1000 : 0));
                    outpak.WriteShort(contentHead);
                    outpak.WriteShort(0); // No Fall Speed.
                }

                // Write Flags
                byte flagcontent = 0;

                if (client.Player.IsWireframe)
                    flagcontent |= 0x01;
                if (client.Player.IsStealthed)
                    flagcontent |= 0x02;
                if (client.Player.IsDiving)
                    flagcontent |= 0x04;
                if (client.Player.IsTorchLighted)
                    flagcontent |= 0x80;

                outpak.WriteByte(flagcontent);
                outpak.WriteByte((byte)(client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
                outpak.WriteByte(client.Player.ManaPercent);
                outpak.WriteByte(client.Player.EndurancePercent);
                byte[] outpakArr = outpak.ToArray();

                GSUDPPacketOut outpak190 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak190.Write(outpakArr, 5, outpakArr.Length - 5);
                outpak190.FillString(client.Player.CharacterClass.Name, 32);
                outpak190.WriteByte((byte)(client.Player.RPFlag ? 1 : 0)); // roleplay flag, if == 1, show name (RP) with gray color
                outpak190.WriteByte(0); // send last byte for 190+ packets

                GSUDPPacketOut outpak1112 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                outpak1112.Write(outpakArr, 5, outpakArr.Length - 5);
                outpak1112.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak1112.WriteByte(0); //outpak1112.WriteByte((con168.Length == 22) ? con168[21] : (byte)0);

                GSUDPPacketOut outpak1124 = new(client.Out.GetPacketCode(eServerPackets.PlayerPosition));
                byte playerAction = 0x00;

                if (client.Player.IsDiving)
                    playerAction |= 0x04;
                if (client.Player.TargetInView)
                    playerAction |= 0x30;
                if (client.Player.GroundTargetInView)
                    playerAction |= 0x08;
                if (client.Player.IsTorchLighted)
                    playerAction |= 0x80;
                if (client.Player.IsStealthed)
                    playerAction |= 0x02;

                ushort playerState = 0;
                outpak1124.WriteFloatLowEndian(client.Player.X);
                outpak1124.WriteFloatLowEndian(client.Player.Y);
                outpak1124.WriteFloatLowEndian(client.Player.Z);
                outpak1124.WriteFloatLowEndian(client.Player.CurrentSpeed);
                outpak1124.WriteFloatLowEndian(fallSpeed);
                outpak1124.WriteShort((ushort) client.SessionID);
                outpak1124.WriteShort(zoneId);
                outpak1124.WriteShort(playerState);
                outpak1124.WriteShort((ushort) (client.Player.Steed?.RiderSlot(client.Player) ?? 0)); // fall damage flag coming in, steed seat position going out
                outpak1124.WriteShort(client.Player.Heading);
                outpak1124.WriteByte(playerAction);
                outpak1124.WriteByte((byte) (client.Player.RPFlag ? 1 : 0));
                outpak1124.WriteByte(0);
                outpak1124.WriteByte((byte) (client.Player.HealthPercent + (client.Player.attackComponent.AttackState ? 0x80 : 0)));
                outpak1124.WriteByte(client.Player.ManaPercent);
                outpak1124.WriteByte(client.Player.EndurancePercent);

                foreach (GamePlayer player in client.Player.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
                {
                    if (player == null || player == client.Player)
                        continue;

                    if ((client.Player.InHouse || player.InHouse) && player.CurrentHouse != client.Player.CurrentHouse)
                        continue;

                    if (client.Player.MinotaurRelic != null)
                    {
                        MinotaurRelic relic = client.Player.MinotaurRelic;
                        if (!relic.Playerlist.Contains(player) && player != client.Player)
                        {
                            relic.Playerlist.Add(player);
                            player.Out.SendMinotaurRelicWindow(client.Player, client.Player.MinotaurRelic.Effect, true);
                        }
                    }

                    if (!client.Player.IsStealthed || player.CanDetect(client.Player))
                    {
                        //forward the position packet like normal!
                        if (player.Client.Version >= GameClient.eClientVersion.Version1124)
                            player.Out.SendUDP(outpak1124);
                        else if (player.Client.Version >= GameClient.eClientVersion.Version1112)
                            player.Out.SendUDP(outpak1112);
                        else if (player.Client.Version >= GameClient.eClientVersion.Version190)
                            player.Out.SendUDP(outpak190);
                    }
                    else
                        player.Out.SendObjectDelete(client.Player); //remove the stealthed player from view
                }
            }
        }

        [Flags]
        private enum State : byte
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
        private enum Action : byte
        {
            TORCH =                 1 << 7,
            TELEPORT =              1 << 6,
            TARGET_IN_VIEW =        (1 << 5) | (1 << 4),
            GROUNT_TARGET_IN_VIEW = 1 << 3,
            DIVING =                1 << 1
        }
    }
}
