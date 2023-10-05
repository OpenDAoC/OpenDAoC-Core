namespace DOL.GS.Commands;

[Command("&stuck",
	ePrivLevel.Player, //minimum privelege level
	"Removes the player from the world and put it to a safe location", //command description
	"/stuck")] //usage
public class StuckCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "stuck"))
			return;

		client.Player.Stuck = true;
		if (!client.Player.Quit(false))
		{
			client.Player.Stuck = false;
		}
	}
}