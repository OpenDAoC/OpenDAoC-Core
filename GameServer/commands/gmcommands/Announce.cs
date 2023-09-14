using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&announce",
        ePrivLevel.GM,
        "GMCommands.Announce.Description",
        "GMCommands.Announce.Usage")]
    public class AnnounceCommandHandler : AbstractCommandHandler, ICommandHandler
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
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.LogAnnounce", message), eChatType.CT_Important, eChatLoc.CL_SystemWindow);

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
                        player.Out.SendMessage(LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.SendAnnounce", message), eChatType.CT_Send, eChatLoc.CL_ChatWindow);

                    break;
                }
                case "center":
                {
                    foreach (GamePlayer player in ClientService.GetPlayers())
                        player.Out.SendMessage(message, eChatType.CT_ScreenCenter, eChatLoc.CL_SystemWindow);

                    break;
                }
                case "confirm":
                {
                    foreach (GamePlayer player in ClientService.GetPlayers())
                        player.Out.SendDialogBox(eDialogCode.SimpleWarning, 0, 0, 0, 0, eDialogType.Ok, true, LanguageMgr.GetTranslation(player.Client, "GMCommands.Announce.ConfirmAnnounce", client.Player.Name, message));

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
}
