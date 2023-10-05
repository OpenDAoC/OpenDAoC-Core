namespace DOL.GS.Commands;

[Command("&quit", new string[] { "&q" }, //command to handle
	ePrivLevel.Player, //minimum privelege level
	"Removes the player from the world", //command description
	"/quit")] //usage
public class QuitCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "quit"))
			return;

		client.Player.Quit(false);
	}
}