namespace DOL.GS.Commands;

[Command("&hood", //command to handle
	ePrivLevel.Player, //minimum privelege level
	"Toggles the hood on and off when wearing a hooded cloak.", //command description
	"/hood")] //usage
public class HoodCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "hood"))
			return;

		client.Player.IsCloakHoodUp = !client.Player.IsCloakHoodUp;
	}
}