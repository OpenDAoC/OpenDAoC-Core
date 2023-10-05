namespace DOL.GS.Commands;

[Command(
	"&bind",
	ePrivLevel.Player,
	"Binds your soul to a bind location, you will start from there after you die and /release",
	"/bind")]
public class BindCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// Method to handle the command and any arguments
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "bind"))
			return;

		client.Player.Bind(false);
	}
}