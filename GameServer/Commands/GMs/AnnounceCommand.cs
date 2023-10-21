using System.Collections.Generic;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.Languages;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
    "&announce",
    EPrivLevel.GM,
    "GMCommands.Announce.Description",
    "GMCommands.Announce.Usage")]
public class AnnounceCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 3)
        {
            DisplaySyntax(client);
            return;
        }

        string message = string.Join(" ", args, 2, args.Length - 2);

        if (message == "")
            return;

        switch (args.GetValue(1).ToString().ToLower())
        {
            case "log":
            {
                foreach (GamePlayer player in ClientService.GetPlayers())
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.LogAnnounce", message), EChatType.CT_Important, EChatLoc.CL_SystemWindow);

                break;
            }
            case "window":
            {
                List<string> messages = new()
                {
                    message
                };

                foreach (GamePlayer player in ClientService.GetPlayers())
                    player.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.WindowAnnounce", player.Name), messages);

                break;
            }
            case "send":
            {
                foreach (GamePlayer player in ClientService.GetPlayers())
                    player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.SendAnnounce", message), EChatType.CT_Send, EChatLoc.CL_ChatWindow);

                break;
            }
            case "center":
            {
                foreach (GamePlayer player in ClientService.GetPlayers())
                    player.Out.SendMessage(message, EChatType.CT_ScreenCenter, EChatLoc.CL_SystemWindow);

                break;
            }
            case "confirm":
            {
                foreach (GamePlayer player in ClientService.GetPlayers())
                    player.Out.SendDialogBox(EDialogCode.SimpleWarning, 0, 0, 0, 0, EDialogType.Ok, true, LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.ConfirmAnnounce", client.Player.Name, message));

                break;
            }
            default:
            {
                DisplaySyntax(client);
                return;
            }
        }
    }
}