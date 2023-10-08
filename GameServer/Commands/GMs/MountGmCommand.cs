namespace DOL.GS.Commands;

[Command(
	"&mountgm",
	EPrivLevel.GM,
	"Mount a steed",
	"/mountgm")]
public class MountGmCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player.IsRiding)
		{
			DisplayMessage(client, "You are already riding a steed!");
			return;
		}

		if (client.Player.TargetObject == null || !(client.Player.TargetObject is GameNPC))
		{
			DisplayMessage(client, "You can't ride THIS!");
			return;
		}

		client.Player.MountSteed((GameNPC) client.Player.TargetObject, false);
	}
}