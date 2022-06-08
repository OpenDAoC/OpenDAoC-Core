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
using DOL.GS.PacketHandler;
using System.Collections;
using System.Collections.Generic;
using DOL.Language;
using DOL.GS.ServerRules;
using System;
using DOL.GS.Utils;

namespace DOL.GS.Commands
{
	[CmdAttribute(
	   "&realmtimer",
	   ePrivLevel.Player,
		 "Displays the players current realmtimer Status", "/realmtimer")]
	public class RealmTimerCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "realmtimer"))
				return;

			string realmname = "None";
			switch ((eRealm)RealmTimer.CurrentRealm(client.Player))
			{
				case eRealm.Albion: 
					realmname = "Albion";
					break;
				case eRealm.Midgard:
				 	realmname = "Midgard";
					break;
				case eRealm.Hibernia:
				 	realmname = "Hibernia";
					break;
				default: 
					realmname = "None";
					break;

			}

			TimeSpan realmtimerminutes = TimeSpan.FromMinutes(RealmTimer.TimeLeftOnTimer(client.Player));
			DisplayMessage(client, "Realm Timer Status. Realm: " + realmname + " Time Left: " + realmtimerminutes.Hours + "h " + realmtimerminutes.Minutes + "m");
			
		}

	}
}
