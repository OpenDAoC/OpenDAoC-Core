using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&gtrange",
	EPrivLevel.Player,
	"Gives a range to a ground target",
	"/gtrange")]
public class GtRangeCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "gtrange"))
			return;

		if (client.Player.GroundTarget != null)
		{
            int range = client.Player.GetDistanceTo( client.Player.GroundTarget );
			client.Out.SendMessage("Range to target: " + range + " units.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}
		else
			client.Out.SendMessage("Range to target: You don't have a ground target set.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}
}