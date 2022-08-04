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
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&speclock",
		ePrivLevel.GM,
		"Set your SpecMod combat modifier to a designated value <0.01 min>",
		"/speclock <value> - where value is a decimal input like 1.10 or 0.85", 
		"/speclock reset - clear value and use normal combat calculations")]
	
	public class SpecLockCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
				

			try
			{
				if (args[1].Equals("reset"))
				{
					client.Player.SpecLock = 0;
					return;
				}

				double input = Double.Parse(args[1]);
				if (input <= 0)
				{
					DisplaySyntax(client);
					return;
				}

				client.Player.SpecLock = input;
			}
			catch (Exception e)
			{
				Console.WriteLine(e + ": " + e.StackTrace);
				DisplaySyntax(client);
			}
		}
	}
}