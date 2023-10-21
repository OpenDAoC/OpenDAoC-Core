namespace Core.GS.Commands;

[Command(
	"&whisper",
	new string[] {"&whis"}, //Important, don't remove this alias, its used for communication with mobs!
	EPrivLevel.Player,
	"Sends a private message to your target if it is close enough",
	"/whisper <message>")]
public class WhisperCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (IsSpammingCommand(client.Player, "whisper", 500))
		{
			DisplayMessage(client, "Slow down! Think before you say each word!");
			return;
		}

		GameObject obj = client.Player.TargetObject;
		if (obj == null)
		{
			DisplayMessage(client, "Select the target you want to whisper to!");
			return;
		}

		if (obj == client.Player)
		{
			DisplayMessage(client, "Hmmmm...you shouldn't talk to yourself!");
			return;
		}

		client.Player.Whisper(obj, string.Join(" ", args, 1, args.Length - 1));
	}
}