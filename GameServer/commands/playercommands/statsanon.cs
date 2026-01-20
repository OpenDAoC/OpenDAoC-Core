using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
    [CmdAttribute(
    "&statsanon",
    ePrivLevel.Player,
    "Hides your statistics",
    "/statsanon")]
    public class StatsAnonHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "statsanon"))
                return;

            if (client == null)
                return;

            client.Player.IgnoreStatistics = !client.Player.IgnoreStatistics;
            string msg;

            if (client.Player.IgnoreStatistics)
                msg = "Your stats are no longer visible to other players.";
            else
                msg = "Your stats are now visible to other players.";

            client.Player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_ChatWindow);
        }
    }
}
