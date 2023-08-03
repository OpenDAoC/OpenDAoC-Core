
using System.Linq;
using DOL.GS.PacketHandler;
using DOL.GS.Housing;

namespace DOL.GS.Commands
{
	[Command(
		"&housefriend",
		EPrivLevel.Player,
		"Invite a specified player to your house", 
        "/housefriend all", 
        "/housefriend player <player>", 
        "/housefriend account <player>", 
        "/housefriend guild <guild> (If there are two or more words enclose them with \" \")")]
	public class HouseFriendCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
            {
                DisplaySyntax(client);
                return;
            }

            if (!client.Player.InHouse)
            {
                client.Out.SendMessage("You need to be in your House to use this command", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                return;
            }

			switch (args[1])
			{
				case "player":
					{
						if (args.Length == 2)
							return;

						if (client.Player.Name == args[2])
							return;

						GameClient targetClient = WorldMgr.GetClientByPlayerNameAndRealm(args[2], 0, true).FirstOrDefault();
						if (targetClient == null)
						{
							client.Out.SendMessage("No players online with that name.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
							return;
						}

						if (client.Player.CurrentHouse.AddPermission(targetClient.Player, EPermissionType.Player, HousingConstants.MinPermissionLevel))
						{
							client.Out.SendMessage("You added " + targetClient.Player.Name + ".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
						break;
					}

                case "account":
                    {
                        if (args.Length == 2)
                            return;

                        if (client.Player.Name == args[2])
                            return;

                        GameClient targetClient = WorldMgr.GetClientByPlayerNameAndRealm(args[2], 0, true).FirstOrDefault();
                        if (targetClient == null)
                        {
                            client.Out.SendMessage("No players online with that name.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

						if (client.Player.CurrentHouse.AddPermission(targetClient.Player, EPermissionType.Account, HousingConstants.MinPermissionLevel))
						{
							client.Out.SendMessage("You added " + targetClient.Player.Name + "'s account.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
                        break;
                    }

                case "guild":
                    {
                        if (args.Length == 2)
                            return;

                        GuildUtil targetGuild = GuildMgr.GetGuildByName(args[2]);
                        if (targetGuild == null)
                        {
                            client.Out.SendMessage("A guild with that name was not found. Don't forget to put longer names in quotes eg: \"My Guild\".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
                            return;
                        }

						if (client.Player.CurrentHouse.AddPermission(targetGuild.Name, EPermissionType.Guild, HousingConstants.MinPermissionLevel))
						{
							client.Out.SendMessage("You added " + targetGuild.Name + ".", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
                        break;
                    
                    }
				case "all":
					{
						if (client.Player.CurrentHouse.AddPermission("All", EPermissionType.All, HousingConstants.MinPermissionLevel))
						{
							client.Out.SendMessage("You added everybody!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						}
						break;
					}
                default:
                    DisplaySyntax(client);
                    break;
			}
		}
	}
}