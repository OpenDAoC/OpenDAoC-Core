using System;
using DOL.Database;
using DOL.GS.PacketHandler;
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
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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
                        if (npcString == string.Empty)
                        {
                            client.Out.SendMessage("You must specify a teleport string to whisper the npc.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        if (npcString != "enter" || npcString != "exit")
                        {
                            client.Out.SendMessage("Valid strings are \"enter\" and \"exit\"", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }
                        
                        if (args[3] == string.Empty)
                        {
                            client.Out.SendMessage("You must specify the teleport type", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        if (args[3] != "in" || args[3] != "out")
                        {
                            client.Out.SendMessage("Valid types are \"in\" and \"out\"", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        var teleportType = string.Empty;
                        if (args[3] == "in")
                        {
                            teleportType = "GateKeeperIn";
                        }
                        else
                        {
                            teleportType = "GateKeeperOut";
                        }

                        var keep = GameServer.KeepManager.GetClosestKeepToSpot(client.Player.CurrentRegionID, client.Player, WorldMgr.VISIBILITY_DISTANCE);
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

            var verification = GameServer.Database.SelectObject<DbKeepDoorTeleport>(DB.Column("KeepID").IsEqualTo(keep.KeepID).And(DB.Column("Text").IsEqualTo(Text).And(DB.Column("Type").IsEqualTo(teleportType))));
            if (verification != null)
            {
                client.Out.SendMessage(String.Format("Teleport ID with same parameter already exists!"), eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }
            DbKeepDoorTeleport teleport = new DbKeepDoorTeleport();
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
