namespace Core.GS.Commands;

[Command("&noqueue", //command to handle
EPrivLevel.Player, //minimum privelege level
"Allows you to disable/enable queuing", "/Noqueue")] //usage
public class NoQueueCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "noqueue"))
			return;

		client.Player.SpellQueue = !client.Player.SpellQueue;

		if (client.Player.SpellQueue)
		{
			DisplayMessage(client, "You are now using the queuing option! To disable queuing use '/noqueue'.");
		}
		else
		{
			DisplayMessage(client, "You are no longer using the queuing option! To enable queuing use '/noqueue'.");

		}
	}
}