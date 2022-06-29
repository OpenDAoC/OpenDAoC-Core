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
    /// A command to manage teleport destinations.
    /// </summary>
    /// <author>Shursan</author>
    [CmdAttribute(
        "&keepdoorteleport",
        ePrivLevel.Admin,
        "Manage keepdoor teleport destinations",
        "'/keepdoorteleport add <enter|exit> <in|out> add a teleport destination"/*,
        "'/keepdoorteleport reload' reload all teleport locations from the db"*/)]
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
                            client.Out.SendMessage("You must specify a teleport string to whisper the npc.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        if (npcString != "enter" || npcString != "exit")
                        {
                            client.Out.SendMessage("Valid strings are \"enter\" and \"exit\"", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        if (args[3] == "")
                        {
                            client.Out.SendMessage("You must specify the teleport type", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args[3] != "in" || args[3] != "out")
                        {
                            client.Out.SendMessage("Valid types are \"in\" and \"out\"", eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
                            client.Out.SendMessage("You need to be inside a keep area.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        AddTeleport(client, npcString, keep, teleportType);
                    }
                    break;

                case "reload":
                    
                    var results = WorldMgr.LoadTeleports();
                    log.Info(results);
                    client.Out.SendMessage(results, eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    
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

            var verification = GameServer.Database.SelectObject<DBKeepDoorTeleport>("KeepID = '" + keep.KeepID + "'" + " AND Text = '" + Text + "'" + "AND Type = '" + teleportType + "'");
            if (verification != null)
            {
                client.Out.SendMessage(String.Format("Teleport ID with same parameter already exists!"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
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
            client.Out.SendMessage(String.Format("KeepDoor Teleport ID [{0}] successfully added.", Text),
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
