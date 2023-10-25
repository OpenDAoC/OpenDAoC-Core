using Core.GS.Enums;
using Core.GS.GameUtils;

namespace Core.GS.Commands;

[Command(
	"&news",
	EPrivLevel.Player,
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