﻿using System;
using Core.Database;
using Core.Database.Tables;
using Core.GS.PacketHandler;

namespace Core.GS.Commands
{
	[Command("&offlinejump",
		EPrivLevel.GM,
		"GMCommands.Offlinejump.Description",
		"GMCommands.Offlinejump.Usage.Jail",
		"GMCommands.Offlinejump.Usage.Capital"
	)]
	public class OfflineJumpCommand : ACommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			try
			{
				if (args.Length == 4 && args[2] == "to" && args[3] == "jail")
				{
					DbCoreCharacter character;
					character = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(args[1]));
					if (character == null)
					{
						client.Out.SendMessage("Character " + args[1] + " not found", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
						return;
					}
					character.Xpos = 33278;
					character.Ypos= 35168;
					character.Zpos = 16220;
					character.Region = 497;
					character.Direction = 2056;
					BindCharacter(character);
					GameServer.Database.SaveObject(character);
					client.Out.SendMessage("Character " + args[1].ToUpperInvariant() + " has been moved to Jail", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
				} 
				else if (args.Length == 4 && args[2] == "to" && args[3] == "capital")
				{
					DbCoreCharacter character;
					character = CoreDb<DbCoreCharacter>.SelectObject(DB.Column("Name").IsEqualTo(args[1]));
					if (character == null)
					{
						client.Out.SendMessage("Character " + args[1] + " not found", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
						return;
					}
					
					switch (character.Realm)
					{
						case 1:
							character.Xpos = 36209;
							character.Ypos = 29843;
							character.Zpos = 7971;
							character.Region = 10;
							character.Direction = 18;
							break;
						case 2:
							character.Xpos = 31619;
							character.Ypos = 28768;
							character.Zpos = 8800;
							character.Region = 101;
							character.Direction = 2201;
							break;
						case 3:
							character.Xpos = 30011;
							character.Ypos = 33138;
							character.Zpos = 7916;
							character.Region = 201;
							character.Direction = 3079;
							break;
						default:
							character.Xpos = 47411;
							character.Ypos= 48694;
							character.Zpos = 25000;
							character.Region = 249;
							character.Direction = 5;
							break;
					}
					BindCharacter(character);
					GameServer.Database.SaveObject(character);
					client.Out.SendMessage("Character " + args[1].ToUpperInvariant() + " has been moved to their Realm's Capital City", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
				} 
				else
				{
					DisplaySyntax(client);
				}
			}
			catch (Exception ex)
			{
				DisplayMessage(client, ex.Message);
			}
		}
		
		public static void BindCharacter(DbCoreCharacter ch)
		{
			ch.BindRegion = ch.Region;
			ch.BindHeading = ch.Direction;
			ch.BindXpos = ch.Xpos;
			ch.BindYpos = ch.Ypos;
			ch.BindZpos = ch.Zpos;
		}
	}
}