namespace DOL.GS.Commands;

[Command(
	"&news",
	ePrivLevel.Player,
	"Show news on social interface",
	"/news")]
public class NewsCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player == null)
			return;

		if (IsSpammingCommand(client.Player, "news"))
			return;

		NewsMgr.DisplayNews(client);
	}
}