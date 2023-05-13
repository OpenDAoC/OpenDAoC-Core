/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;
using DOL.GS.Keeps;

namespace DOL.GS.Commands
{
    /// <summary>
    /// A command to manage keep door teleport destinations.
    /// </summary>
    /// <author>Shursan</author>
    [CmdAttribute(
        "&keepdoorteleport",
        // Message: '/keepdoorteleport' - Manages teleport destinations for keep door gatekeepers.
        "AdminCommands.DoorTeleport.CmdList.Description",
        // Message: <----- '/{0}' Command {1}----->
        "AllCommands.Header.General.Commands",
        // Required minimum privilege level to use the command
        ePrivLevel.Admin,
        // Message: Manages teleport locations for keep door gatekeepers.
        "AdminCommands.DoorTeleport.Description",
        // Message: /keepdoorteleport add <enter|exit> <in|out>
        "AdminCommands.DoorTeleport.Syntax.Add",
        // Message: Adds a keep door teleport location for entering or exiting a keep.
        "AdminCommands.DoorTeleport.Usage.Add",
        // Message: /keepdoorteleport reload
        "AdminCommands.DoorTeleport.Syntax.Reload",
        // Message: Loads any new changes from the database into the server's cache.
        "AdminCommands.DoorTeleport.Usage.Reload"
    )]
    public class KeepDoorTeleportCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Handle command.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 4)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "add":
                {
                        var npcString = args[2];
                        if (npcString == "")
                        {
                            // Message: You must specify a string value for players to whisper to the gatekeeper. Accepted values include "enter" or "exit".
                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.TeleportString", null);
                            return;
                        }
                        
                        if (npcString != "enter" || npcString != "exit")
                        {
                            // Message: The string value entered is not supported. Accepted values include "enter" or "exit".
                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.ValidStrings", null);
                            return;
                        }
                        
                        if (args[3] == "")
                        {
                            // Message: You must specify a teleport type. Accepted values include "in" or "out".
                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.TeleportType", null);
                            return;
                        }

                        if (args[3] != "in" || args[3] != "out")
                        {
                            // Message: The teleport type entered is not supported. Accepted values include "in" or "out".
                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.ValidTypes", null);
                            return;
                        }

                        var teleportType = "";
                        if (args[3] == "in")
                        {
                            teleportType = "GateKeeperIn";
                        }
                        else
                        {
                            teleportType = "GateKeeperOut";
                        }

                        var keep = GameServer.KeepManager.GetKeepCloseToSpot(client.Player.CurrentRegionID, client.Player, WorldMgr.VISIBILITY_DISTANCE);
                        if (keep == null)
                        {
                            // Message: You must be inside a keep area to execute this command.
                            ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.KeepArea", null);
                            return;
                        }

                        AddTeleport(client, npcString, keep, teleportType);
                    }
                    break;

                case "reload":
                    
                    var results = WorldMgr.LoadTeleports();
                    log.Info(results);

                    ChatUtil.SendTypeMessage((int)eMsg.Success, client, results);
                    
                    break;

                default:
                    DisplaySyntax(client);
                    break;
            }
        }

        /// <summary>
        /// Add a new teleport destination in memory and save to database, if
        /// successful.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="teleportID"></param>
        /// <param name="type"></param>
        private void AddTeleport(GameClient client, String Text, AbstractGameKeep keep, string teleportType)
        {
            GamePlayer player = client.Player;

            var verification = GameServer.Database.SelectObject<DBKeepDoorTeleport>(DB.Column("KeepID").IsEqualTo(keep.KeepID).And(DB.Column("Text").IsEqualTo(Text).And(DB.Column("Type").IsEqualTo(teleportType))));
            
            if (verification != null)
            {
                // Message: A teleport ID with the same parameter(s) already exists!
                ChatUtil.SendTypeMessage((int)eMsg.Error, client, "AdminCommands.DoorTeleport.Err.SameParameter", null);
                return;
            }
            
            DBKeepDoorTeleport teleport = new DBKeepDoorTeleport();
            teleport.Text = Text;
            teleport.Region = player.CurrentRegion.ID;
            teleport.X = player.X;
            teleport.Y = player.Y;
            teleport.Z = player.Z;
            teleport.Heading = player.Heading;
            teleport.KeepID = keep.KeepID;
            teleport.CreateInfo = keep.Name;
            teleport.TeleportType = teleportType;

            GameServer.Database.AddObject(teleport);
            
            // Message: You have successfully added a keep door teleport location (ID {0})!
            ChatUtil.SendTypeMessage((int)eMsg.Success, client, "AdminCommands.DoorTeleport.Msg.TeleportAdded", null);
        }
    }
}
