using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&rp",
	EPrivLevel.Player,
	"toggle receiving realm points",
	"/rp <on/off>")]
public class RpCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (IsSpammingCommand(client.Player, "rp"))
			return;


		if (client.Player.Level < 40)
		{
			DisplayMessage(client, "This command is only available to players above level 39.");
			return;
		}

		switch (args[1].ToLower())
		{
			case "on":
				client.Player.GainRP = true;
				client.Out.SendMessage("Your rp flag is ON. You will gain realm points. Use '/rp off' to stop gaining realm points.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				break;
			case "off":
				client.Player.GainRP = false;
				client.Out.SendMessage("Your rp flag is OFF. You will no longer gain realm points. Use '/rp on' to start gaining realm points again.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				break;
		}
	}
}