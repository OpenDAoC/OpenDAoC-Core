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
using System.Reflection;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute("&serverinfo", //command to handle
		ePrivLevel.Player, //minimum privelege level
		"Shows information about the server", //command description
		"/serverinfo")] //usage
	public class ServerInfoCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			client.Out.SendMessage("Atlas", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			var an = Assembly.GetAssembly(typeof(GameServer)).GetName();
			client.Out.SendMessage("Online: " + WorldMgr.GetAllPlayingClientsCount(), eChatType.CT_System, eChatLoc.CL_SystemWindow);
			if (client.Player == null) return;
			var uptime = DateTime.Now.Subtract(GameServer.Instance.StartupTime);
				
			var sec = uptime.TotalSeconds;
			var min = Convert.ToInt64(sec) / 60;
			var hours = min / 60;
			var days = hours / 24;
				
			DisplayMessage(client, $"Uptime: {days}d {hours % 24}h {min % 60}m {sec % 60:00}s");
		}
	}
}