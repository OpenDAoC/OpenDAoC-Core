﻿using DOL.GS.PacketHandler;
using System.Collections;
using System.Collections.Generic;
using DOL.Language;
using DOL.GS.ServerRules;
using System;
using Core.GS.GameUtils;
using DOL.GS.Utils;

namespace DOL.GS.Commands
{
	[Command(
	   "&realmtimer",
	   EPrivLevel.Player,
		 "Displays the players current realmtimer Status", "/realmtimer")]
	public class RealmTimerCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (IsSpammingCommand(client.Player, "realmtimer"))
				return;

			string realmname = "None";
			switch ((ERealm)RealmTimer.CurrentRealm(client.Player))
			{
				case ERealm.Albion: 
					realmname = "Albion";
					break;
				case ERealm.Midgard:
				 	realmname = "Midgard";
					break;
				case ERealm.Hibernia:
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