using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&pray",
	EPrivLevel.Player,
	"You can pray on your gravestones to get some experience back",
	"/pray")]
public class PrayCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "pray"))
			return;

		client.Player.Pray();
	}
}