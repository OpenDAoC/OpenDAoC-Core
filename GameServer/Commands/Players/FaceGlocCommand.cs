using System;
using Core.GS.Enums;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&facegloc",
	EPrivLevel.Player,
	"Turns and faces your character into the direction of the x, y coordinates provided (using DOL region global coordinates).",
	"/facegloc [x] [y]")]
public class FaceGlocCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "facegloc"))
			return;

		if (args.Length < 3)
		{
			client.Out.SendMessage("Please enter X and Y coordinates.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		int x, y;
		try
		{
			x = Convert.ToInt32(args[1]);
			y = Convert.ToInt32(args[2]);
		}
		catch (Exception)
		{
			client.Out.SendMessage("Please enter X and Y coordinates.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

        ushort direction = client.Player.GetHeading( new Point2D( x, y ) );
		client.Player.Heading = direction;
		client.Out.SendPlayerJump(true);
	}
}