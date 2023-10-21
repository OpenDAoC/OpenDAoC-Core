namespace DOL.GS.Commands;

[Command("&namemount", EPrivLevel.Player,"Name your hourse","/namemount")]
public class NameMountCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player == null || args.Length < 2)
			return;

		if (IsSpammingCommand(client.Player, "namemount"))
			return;

		string horseName = args[1];
		if (!client.Player.HasHorse)
			return;
		client.Player.ActiveHorse.Name = horseName;
	}
}