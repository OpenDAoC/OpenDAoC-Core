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
	[CmdAttribute(
	  "&anonymous",
	  new [] {"&anon"},
	  ePrivLevel.Player,
	  // Displays next to the command when '/cmd' is entered
	  "Enables/disables anonymous mode, which hides you from player searches (e.g., '/who').",
	  // Syntax: '/anonymous' or '/anon' - Enables/disables anonymous mode, which hides you from player searches (e.g., '/who').
	  "PLCommands.Anonymous.Syntax.Anon")]
	public class AnonymousCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		/// <summary>
		/// Change Player Anonymous Flag on Command
		/// </summary>
		/// <param name="client"></param>
		/// <param name="args"></param>
		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player == null)
				return;
			
			// If anonymous mode is disabled from the 'serverproperty' table
			if (client.Account.PrivLevel == 1 && ServerProperties.Properties.ANON_MODIFIER == -1)
			{
				// Message: Anonymous mode is currently disabled.
				ChatUtil.SendSystemMessage(client, "PLCommands.Anonymous.Err.Disabled", null);
				return;
			}

			// Sets the default value for anonymous mode on a character (off)
			client.Player.IsAnonymous = !client.Player.IsAnonymous;

			// Enable anonymous mode
			if (client.Player.IsAnonymous)
				// Message: You are now anonymous.
				ChatUtil.SendErrorMessage(client, "PLCommands.Anonymous.Msg.On", null);
			// Disable anonymous mode
			else
				// Message: You are no longer anonymous.
				ChatUtil.SendErrorMessage(client, "PLCommands.Anonymous.Msg.Off", null);
		}
	}
}
