using System;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&walk",
	EPrivLevel.GM,
	"Commands a npc to walk!",
	"'/walk <xoff> <yoff> <zoff> <speed>' to make the npc walk to x+xoff, y+yoff, z+zoff")]
[Command(
	"&stop",
	EPrivLevel.GM,
	"Stops the npc's movement!",
	"'/stop' to stop the target mob")]
public class WalkStopCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		GameNpc targetNPC = null;
		if (client.Player.TargetObject != null && client.Player.TargetObject is GameNpc)
			targetNPC = (GameNpc) client.Player.TargetObject;

		if (args.Length == 1 && args[0] == "&stop")
		{
			if (targetNPC == null)
			{
				client.Out.SendMessage("Type /stop to stop your target npc from moving", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			targetNPC.StopMoving();
			return;
		}
		if (args.Length < 4)
		{
			client.Out.SendMessage("Usage:", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage("'/walk <xoff> <yoff> <zoff> <speed>'", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		if (targetNPC == null)
		{
			client.Out.SendMessage("Type /walk for command overview", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		int xoff = 0;
		int yoff = 0;
		int zoff = 0;
		short speed = 50;

		try
		{
			xoff = Convert.ToInt16(args[1]);
			yoff = Convert.ToInt16(args[2]);
			zoff = Convert.ToInt16(args[3]);
			speed = Convert.ToInt16(args[4]);
		}
		catch (Exception)
		{
			client.Out.SendMessage("Type /walk for command overview", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		targetNPC.WalkTo(new Point3D(targetNPC.X + xoff, targetNPC.Y + yoff, targetNPC.Z + zoff), speed);
	}
}