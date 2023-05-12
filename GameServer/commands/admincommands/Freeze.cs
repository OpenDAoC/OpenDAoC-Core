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

using System;
using DOL.GS;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles all user-based interaction for the '/freeze' command
	/// </summary>
	[CmdAttribute(
		// Enter '/freeze' to list all associated subcommands
		"&freeze",
		// Message: '/freeze' - Triggers a brief pause in the region time for testing purposes.
		"AdminCommands.Freeze.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Triggers a brief pause in the region time for testing purposes.
		"AdminCommands.Freeze.Description",
		// Syntax: /freeze <seconds>
		"AdminCommands.Freeze.Syntax.Freeze",
		// Message: Freezes the region time for the specified number of seconds.
		"AdminCommands.Freeze.Usage.Freeze"
	)]
	public class Freeze : AbstractCommandHandler, ICommandHandler
	{
		private int delay = 0;
		
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists all '/freeze' type commands' syntax (see section above)
				DisplaySyntax(client);
				return;
			}
			
			if (client != null && client.Player != null)
			{
				try
				{
					delay = Convert.ToInt32(args[1]);
					new RegionTimer(client.Player, FreezeCallback).Start(1);
					
					// Message: The region time will now stop for {0} seconds. Please stand by...
					ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "AdminCommands.Freeze.Msg.Freezing", delay);
				}
				catch
				{
					// Message: The command did not work as expected! Please make sure you entered a numeric value to indicate the number of seconds to freeze the region timer.
					ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "AdminCommands.Freeze.Err.Freeze", null);
				}
			}
		}
		
		private int FreezeCallback(RegionTimer timer)
		{
			System.Threading.Thread.Sleep(delay * 1000);
			return 0;
		}

	}
}
