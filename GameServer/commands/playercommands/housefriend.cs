using DOL.GS.Housing;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&housefriend",
        ePrivLevel.Player,
        "Invite a specified player to your house", 
        "/housefriend all", 
        "/housefriend player <player>", 
        "/housefriend account <player>", 
        "/housefriend guild <guild> (If there are two or more words enclose them with \" \")")]
    public class HousefriendCommandHandler : AbstractCommandHandler, ICommandHandler
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
                client.Out.SendMessage("You need to be in your House to use this command", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                return;
            }

            switch (args[1])
            {
                case "player":
                {
                    if (TryAddPlayer(args, PermissionType.Player, out string playerName))
                        client.Out.SendMessage($"You added {playerName}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    break;
                }
                case "account":
                {
                    if (TryAddPlayer(args, PermissionType.Account, out string playerName))
                        client.Out.SendMessage($"You added {playerName}'s account.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    break;
                }
                case "guild":
                {
                    if (args.Length == 2)
                        return;

                    Guild targetGuild = GuildMgr.GetGuildByName(args[2]);

                    if (targetGuild == null)
                    {
                        client.Out.SendMessage("A guild with that name was not found. Don't forget to put longer names in quotes eg: \"My Guild\".", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                        return;
                    }

                    if (client.Player.CurrentHouse.AddPermission(targetGuild.Name, PermissionType.Guild, HousingConstants.MinPermissionLevel))
                        client.Out.SendMessage($"You added {targetGuild.Name}.", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    break;
                }
                case "all":
                {
                    if (client.Player.CurrentHouse.AddPermission("All", PermissionType.All, HousingConstants.MinPermissionLevel))
                        client.Out.SendMessage("You added everybody!", eChatType.CT_System, eChatLoc.CL_SystemWindow);

                    break;
                }
                default:
                {
                    DisplaySyntax(client);
                    break;
                }
            }

            bool TryAddPlayer(string[] args, PermissionType permissionType, out string playerName)
            {
                playerName = string.Empty;

                if (args.Length == 2)
                    return false;

                GamePlayer targetPlayer = ClientService.GetPlayerByPartialName(args[2], out ClientService.PlayerGuessResult result);

                if (client.Player == targetPlayer)
                {
                    client.Out.SendMessage("You cannot use this command on yourself.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                if (result is ClientService.PlayerGuessResult.NOT_FOUND or ClientService.PlayerGuessResult.FOUND_MULTIPLE)
                {
                    client.Out.SendMessage("No players online with that name.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
                    return false;
                }

                playerName = targetPlayer.Name;
                return client.Player.CurrentHouse.AddPermission(targetPlayer, permissionType, HousingConstants.MinPermissionLevel);
            }
        }
    }
}
