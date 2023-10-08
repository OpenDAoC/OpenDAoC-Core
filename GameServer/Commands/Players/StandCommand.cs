namespace DOL.GS.Commands;

[Command("&stand", EPrivLevel.Player, "Stands up when sitting", "/stand")]
public class StandCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (!IsSpammingCommand(client.Player, "sitstand"))
		{
			client.Player.Sit(false);
		}
	}
}