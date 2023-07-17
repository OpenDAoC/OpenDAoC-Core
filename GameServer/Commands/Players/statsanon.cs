
// By Daeli
using System;
using log4net;

using DOL.GS;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
	"&statsanon",
	EPrivLevel.Player,
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

			string msg;

			client.Player.StatsAnonFlag = !client.Player.StatsAnonFlag;
			if (client.Player.StatsAnonFlag)
				msg = "Your stats are no longer visible to other players.";
			else
				msg = "Your stats are now visible to other players.";

			client.Player.Out.SendMessage(msg, EChatType.CT_System, EChatLoc.CL_ChatWindow);
		}
	}
}