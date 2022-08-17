/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */
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
	[CmdAttribute(
		"&faceflag",
		ePrivLevel.Player,
		"Turns and faces your character into the direction of the x, y coordinates provided (using Mythic zone coordinates).",
		"/faceflag [1|2|3|4]")]
	public class FlagFaceCommandHandler : AbstractCommandHandler,ICommandHandler
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
					eChatType.CT_System,
					eChatLoc.CL_SystemWindow
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
				client.Out.SendMessage("Please enter a valid flag number.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}

			var flag = ConquestService.ConquestManager.ActiveObjective.GetObjective(flagnum);
			Console.WriteLine($"flag {flag.FlagObject} | player region {client.Player.CurrentRegionID} | flag region {flag.FlagObject.CurrentRegionID}");

			if (flag == null)
			{
				client.Out.SendMessage("Please enter a valid flag number.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			} 

			if (client.Player.CurrentRegionID != flag.FlagObject.CurrentRegionID)
			{
				client.Out.SendMessage("You must be in the same zone as the flag.", eChatType.CT_System, eChatLoc.CL_SystemWindow);
				return;
			}
			
			ushort direction = client.Player.GetHeading(flag.FlagObject);
			client.Player.Heading = direction;
			client.Out.SendPlayerJump(true);
		}
	}
}