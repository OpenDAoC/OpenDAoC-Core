/*
 * Author:	Ogre <ogre@videogasm.com>
 * Rev:		$Id: faceloc.cs,v 1.6 2005/05/10 13:36:38 noret Exp $
 *
 * Desc:	Implements /faceloc command
 *
 */

using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[Command(
		"&faceflag",
		EPrivLevel.Player,
		"Turns and faces your character into the direction of the x, y coordinates provided (using Mythic zone coordinates).",
		"/faceflag [1|2|3|4]")]
	public class FaceFlagCommand : AbstractCommandHandler,ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "faceflag"))
				return;

			if (client.Player.IsTurningDisabled)
			{
				DisplayMessage(client, "You can't use this command now!");
				return;
			}

			if (args.Length < 2)
			{
				client.Out.SendMessage
					(
					"Please enter flag number.",
					EChatType.CT_System,
					EChatLoc.CL_SystemWindow
					);
				return;
			}

			int flagnum = 0;
			try
			{
				flagnum = System.Convert.ToInt32(args[1]);
			}
			catch
			{
				client.Out.SendMessage("Please enter a valid flag number.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			var flag = ConquestService.ConquestManager.ActiveObjective.GetObjective(flagnum);
			Console.WriteLine($"flag {flag.FlagObject} | player region {client.Player.CurrentRegionID} | flag region {flag.FlagObject.CurrentRegionID}");

			if (flag == null)
			{
				client.Out.SendMessage("Please enter a valid flag number.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			} 

			if (client.Player.CurrentRegionID != flag.FlagObject.CurrentRegionID)
			{
				client.Out.SendMessage("You must be in the same zone as the flag.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			
			ushort direction = client.Player.GetHeading(flag.FlagObject);
			client.Player.Heading = direction;
			client.Out.SendPlayerJump(true);
		}
	}
}