using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
"&statsanon",
EPrivLevel.Player,
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

		client.Player.Out.SendMessage(msg, EChatType.CT_System, EChatLoc.CL_ChatWindow);
	}
}