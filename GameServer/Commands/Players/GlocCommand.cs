namespace Core.GS.Commands;

[Command("&gloc", //command to handle
	EPrivLevel.Player, //minimum privelege level
	"Show the current coordinates", //command description
	"/gloc")] //command usage
public class GlocCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "gloc"))
			return;

		DisplayMessage(client, string.Format("You are at X:{0} Y:{1} Z:{2} Heading:{3} Region:{4} {5} Zone:{6}",
			client.Player.X, client.Player.Y, client.Player.Z, client.Player.Heading, client.Player.CurrentRegionID,
			client.Player.CurrentRegion is BaseInstance ? string.Format("Skin:{0}", client.Player.CurrentRegion.Skin) : "", client.Player.CurrentZone.ID));
	}
}