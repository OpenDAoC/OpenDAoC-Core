﻿using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&xpstats",
	EPrivLevel.Player,
	"Toggle showing XP statistics",
	"/xpstats <off|on|verbose>")]
public class XpStatsCommand : ACommandHandler, ICommandHandler {
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (IsSpammingCommand(client.Player, "xpstats"))
			return;

		if (args[1].ToLower().Equals("on"))
		{
			client.Player.XPLogState = EXpLogState.On;
			client.Out.SendMessage("You will now see detailed experience gain stats. Use '/xpstats off' to stop seeing these details.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		else if (args[1].ToLower().Equals("off"))
		{
			client.Player.XPLogState = EXpLogState.Off;
			client.Out.SendMessage("You will no longer see detailed experience gain stats. Use '/xpstats on' to see these details once more.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
		else if (args[1].ToLower().Equals("verbose"))
		{
			client.Player.XPLogState = EXpLogState.Verbose;
			client.Out.SendMessage("You will see verbose experience gain stats. Use '/xpstats off' to stop seeing these details.", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
		}
	}
}