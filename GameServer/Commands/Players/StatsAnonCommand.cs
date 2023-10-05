using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
"&statsanon",
ePrivLevel.Player,
"Hides your statistics",
"/statsanon")]
public class StatsAnonCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "statsanon"))
			return;

		if (client == null)
			return;

		string msg;

		client.Player.StatsAnonFlag = !client.Player.StatsAnonFlag;
		if (client.Player.StatsAnonFlag)
			msg = "Your stats are no longer visible to other players.";
		else
			msg = "Your stats are now visible to other players.";

		client.Player.Out.SendMessage(msg, eChatType.CT_System, eChatLoc.CL_ChatWindow);
	}
}