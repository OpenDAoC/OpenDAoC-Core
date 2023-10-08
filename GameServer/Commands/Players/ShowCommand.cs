namespace DOL.GS.Commands;

[Command(
	"&show",
	EPrivLevel.Player,
	"Show all your cards to the other players (all cards become 'up').",
	"/show")]
public class ShowCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		CardMgr.Show(client);
	}
}