using System;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
    /// <summary>
    /// A command to manage teleport destinations.
    /// </summary>
	[Command(
		"&teleport",
		EPrivLevel.GM,
        "Manage teleport destinations",
        "'/teleport add <ID> <type>' add a teleport destination",
		"'/teleport reload' reload all teleport locations from the db")]
    public class TeleportCommand : ACommandHandler, ICommandHandler
    {
		private static readonly ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
        /// Handle command.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="args"></param>
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length < 2)
            {
                DisplaySyntax(client);
                return;
            }

            switch (args[1].ToLower())
            {
                case "add":
                    {
                        if (args.Length < 3)
                        {
                            DisplaySyntax(client);
                            return;
                        }

                        if (args[2] == "")
                        {
                            client.Out.SendMessage("You must specify a teleport ID.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                            return;
                        }

                        String teleportType = (args.Length < 4)
                            ? "" : args[3];

                        AddTeleport(client, args[2], teleportType);
                    }
                    break;

				case "reload":

					string results = WorldMgr.LoadTeleports();
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
        private void AddTeleport(GameClient client, String teleportID, String type)
        {
            GamePlayer player = client.Player;
            ERealm realm = player.Realm;

            if (WorldMgr.GetTeleportLocation(realm, String.Format("{0}:{1}", type, teleportID)) != null)
            {
                client.Out.SendMessage(String.Format("Teleport ID [{0}] already exists!", teleportID), 
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            DbTeleport teleport = new DbTeleport();
            teleport.TeleportID = teleportID;
            teleport.Realm = (int)realm;
            teleport.RegionID = player.CurrentRegion.ID;
            teleport.X = player.X;
            teleport.Y = player.Y;
            teleport.Z = player.Z;
            teleport.Heading = player.Heading;
            teleport.Type = type;

            if (!WorldMgr.AddTeleportLocation(teleport))
            {
                client.Out.SendMessage(String.Format("Failed to add teleport ID [{0}] in memory!", teleportID), 
                    eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            GameServer.Database.AddObject(teleport);
            client.Out.SendMessage(String.Format("Teleport ID [{0}] successfully added.", teleportID),
                eChatType.CT_System, eChatLoc.CL_SystemWindow);
        }
    }
}
