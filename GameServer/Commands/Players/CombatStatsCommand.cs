using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&combatstats",
	EPrivLevel.Player,
	"toggle receiving experience points",
	"/combatstats <on/off>")]
public class CombatStatsCommand : ACommandHandler, ICommandHandler {
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length < 2)
		{
			DisplaySyntax(client);
			return;
		}

		if (IsSpammingCommand(client.Player, "combatstats"))
			return;

		if (args[1].ToLower().Equals("on"))
		{
			client.Player.UseDetailedCombatLog = true;
			client.Out.SendMessage("You will now see detailed combat stats. Use '/combatstats off' to stop seeing these details.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
		}
		else if (args[1].ToLower().Equals("off"))
		{
			client.Player.UseDetailedCombatLog = false;
			client.Out.SendMessage("You will no longer see detailed combat stats. Use '/combatstats on' to see these details once more.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
		}
	}
}