using System;
using System.Collections.Generic;
using Core.Database;
using Core.GS.PacketHandler;
using Core.GS.PacketHandler.Client.v168;

namespace Core.GS.Commands
{
	[Command(
		"&door",
		EPrivLevel.GM,
		"GMCommands.door.Description",
		"'/door show' toggle enable/disable add dialog when targeting doors",
		"GMCommands.door.Add",
		"GMCommands.door.Update",
		"GMCommands.door.Delete",
		"GMCommands.door.Name",
		"GMCommands.door.Level",
		"GMCommands.door.Realm",
		"GMCommands.door.Guild",
		"'/door sound <soundid>'",
		"GMCommands.door.Info",
		"GMCommands.door.Heal",
		"GMCommands.door.Locked",
		"GMCommands.door.Unlocked")]
	public class DoorCommand : ACommandHandler, ICommandHandler
	{
		private int DoorID;
		private int doorType;
		private string Realmname;
		private string statut;

		#region ICommandHandler Members

		public void OnCommand(GameClient client, string[] args)
		{
			GameDoor targetDoor = null;

			if (args.Length > 1 && args[1] == "show" && client.Player != null)
			{
				if (client.Player.TempProperties.GetProperty(DoorMgr.WANT_TO_ADD_DOORS, false))
				{
					client.Player.TempProperties.RemoveProperty(DoorMgr.WANT_TO_ADD_DOORS);
					client.Out.SendMessage("You will no longer be shown the add door dialog.", EChatType.CT_System,
					                       EChatLoc.CL_SystemWindow);
				}
				else
				{
					client.Player.TempProperties.SetProperty(DoorMgr.WANT_TO_ADD_DOORS, true);
					client.Out.SendMessage("You will now be shown the add door dialog if door is not found in the DB.",
					                       EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}

				return;
			}

			if (client.Player.CurrentRegion.IsInstance)
			{
				client.Out.SendMessage("You can't add doors inside an instance.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject == null)
			{
				client.Out.SendMessage("You must target a door", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject != null &&
			    (client.Player.TargetObject is GameNpc || client.Player.TargetObject is GamePlayer))
			{
				client.Out.SendMessage("You must target a door", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}

			if (client.Player.TargetObject != null && client.Player.TargetObject is GameDoor)
			{
				targetDoor = (GameDoor) client.Player.TargetObject;
				DoorID = targetDoor.DoorID;
				doorType = targetDoor.DoorID/100000000;
			}

			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1])
			{
				case "name":
					name(client, targetDoor, args);
					break;
				case "guild":
					guild(client, targetDoor, args);
					break;
				case "level":
					level(client, targetDoor, args);
					break;
				case "realm":
					realm(client, targetDoor, args);
					break;
				case "info":
					info(client, targetDoor);
					break;
				case "heal":
					heal(client, targetDoor);
					break;
				case "locked":
					locked(client, targetDoor);
					break;
				case "unlocked":
					unlocked(client, targetDoor);
					break;
				case "kill":
					kill(client, targetDoor, args);
					break;
				case "delete":
					delete(client, targetDoor);
					break;
				case "add":
					add(client, targetDoor);
					break;
				case "update":
					update(client, targetDoor);
					break;
				case "sound":
					sound(client, targetDoor, args);
					break;

				default:
					DisplaySyntax(client);
					return;
			}
		}

		#endregion

		private void add(GameClient client, GameDoor targetDoor)
		{
			var DOOR = CoreDb<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(DoorID));

			if (DOOR != null)
			{
				client.Out.SendMessage("The door is already in the database", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (DOOR == null)
			{
				if (doorType != 7 && doorType != 9)
				{
					var door = new DbDoor();
					door.ObjectId = null;
					door.InternalID = DoorID;
					door.Name = "door";
					door.Type = DoorID/100000000;
					door.Level = 20;
					door.Realm = 6;
					door.X = targetDoor.X;
					door.Y = targetDoor.Y;
					door.Z = targetDoor.Z;
					door.Heading = targetDoor.Heading;
					door.Health = 2545;
					GameServer.Database.AddObject(door);
					(targetDoor).AddToWorld();
					client.Player.Out.SendMessage("Added door ID:" + DoorID + "to the database", EChatType.CT_Important,
					                              EChatLoc.CL_SystemWindow);
					//DoorMgr.Init( );
					return;
				}
			}
		}

		private void update(GameClient client, GameDoor targetDoor)
		{
			delete(client, targetDoor);

			if (targetDoor != null)
			{
				if (doorType != 7 && doorType != 9)
				{
					var door = new DbDoor();
					door.ObjectId = null;
					door.InternalID = DoorID;
					door.Name = "door";
					door.Type = DoorID/100000000;
					door.Level = targetDoor.Level;
					door.Realm = (byte) targetDoor.Realm;
					door.Health = targetDoor.Health;
					door.Locked = targetDoor.Locked;
					door.X = client.Player.X;
					door.Y = client.Player.Y;
					door.Z = client.Player.Z;
					door.Heading = client.Player.Heading;
					GameServer.Database.AddObject(door);
					(targetDoor).AddToWorld();
					client.Player.Out.SendMessage("Added door " + DoorID + " to the database", EChatType.CT_Important,
					                              EChatLoc.CL_SystemWindow);
					return;
				}
			}
		}

		private void delete(GameClient client, GameDoor targetDoor)
		{
			var DOOR = CoreDb<DbDoor>.SelectObject(DB.Column("InternalID").IsEqualTo(DoorID));

			if (DOOR != null)
			{
				GameServer.Database.DeleteObject(DOOR);
				client.Out.SendMessage("Door removed", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
			if (DOOR == null)
			{
				client.Out.SendMessage("This door doesn't exist in the database", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				return;
			}
		}


		private void name(GameClient client, GameDoor targetDoor, string[] args)
		{
			string doorName = "";

			if (args.Length > 2)
				doorName = String.Join(" ", args, 2, args.Length - 2);

			if (doorName != "")
			{
				targetDoor.Name = CheckName(doorName, client);
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door name to " + targetDoor.Name, EChatType.CT_System,
				                       EChatLoc.CL_SystemWindow);
			}
			else
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void sound(GameClient client, GameDoor targetDoor, string[] args)
		{
			uint doorSound;

			try
			{
				if (args.Length > 2)
				{
					doorSound = Convert.ToUInt16(args[2]);
					targetDoor.Flag = doorSound;
					targetDoor.SaveIntoDatabase();
					client.Out.SendMessage("You set the door sound to " + doorSound, EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				else
				{
					DisplaySyntax(client, args[1]);
				}
			}
			catch
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void guild(GameClient client, GameDoor targetDoor, string[] args)
		{
			string guildName = "";

			if (args.Length > 2)
				guildName = String.Join(" ", args, 2, args.Length - 2);

			if (guildName != "")
			{
				targetDoor.GuildName = CheckGuildName(guildName, client);
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door guild to " + targetDoor.GuildName, EChatType.CT_System,
				                       EChatLoc.CL_SystemWindow);
			}
			else
			{
				if (targetDoor.GuildName != "")
				{
					targetDoor.GuildName = "";
					targetDoor.SaveIntoDatabase();
					client.Out.SendMessage("Door guild removed", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				else
					DisplaySyntax(client, args[1]);
			}
		}

		private void level(GameClient client, GameDoor targetDoor, string[] args)
		{
			byte level;

			try
			{
				level = Convert.ToByte(args[2]);
				targetDoor.Level = level;
				targetDoor.Health = targetDoor.MaxHealth;
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door level to " + targetDoor.Level, EChatType.CT_System,
				                       EChatLoc.CL_SystemWindow);
			}
			catch (Exception)
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void realm(GameClient client, GameDoor targetDoor, string[] args)
		{
			byte realm;

			try
			{
				realm = Convert.ToByte(args[2]);
				targetDoor.Realm = (ERealm) realm;
				targetDoor.SaveIntoDatabase();
				client.Out.SendMessage("You changed the door realm to " + targetDoor.Realm, EChatType.CT_System,
				                       EChatLoc.CL_SystemWindow);
			}
			catch (Exception)
			{
				DisplaySyntax(client, args[1]);
			}
		}

		private void info(GameClient client, GameDoor targetDoor)
		{
			if (targetDoor.Realm == ERealm.None)
				Realmname = "None";

			if (targetDoor.Realm == ERealm.Albion)
				Realmname = "Albion";

			if (targetDoor.Realm == ERealm.Midgard)
				Realmname = "Midgard";

			if (targetDoor.Realm == ERealm.Hibernia)
				Realmname = "Hibernia";

			if (targetDoor.Realm == ERealm.Door)
				Realmname = "All";

			if (targetDoor.Locked == 1)
				statut = " Locked";

			if (targetDoor.Locked == 0)
				statut = " Unlocked";

			int doorType = DoorRequestHandler.m_handlerDoorID/100000000;

			var info = new List<string>();

			info.Add(" + Door Info :  " + targetDoor.Name);
			info.Add("  ");
			info.Add(" + Name : " + targetDoor.Name);
			info.Add(" + ID : " + DoorID);
			info.Add(" + Realm : " + (int) targetDoor.Realm + " : " + Realmname);
			info.Add(" + Level : " + targetDoor.Level);
			info.Add(" + Guild : " + targetDoor.GuildName);
			info.Add(" + Health : " + targetDoor.Health + " / " + targetDoor.MaxHealth);
			info.Add(" + Statut : " + statut);
			info.Add(" + Type : " + doorType);
			info.Add(" + X : " + targetDoor.X);
			info.Add(" + Y : " + targetDoor.Y);
			info.Add(" + Z : " + targetDoor.Z);
			info.Add(" + Heading : " + targetDoor.Heading);

			client.Out.SendCustomTextWindow("Door Information", info);
		}

		private void heal(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Health = targetDoor.MaxHealth;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("You change the door health to " + targetDoor.Health, EChatType.CT_System,
			                       EChatLoc.CL_SystemWindow);
		}

		private void locked(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Locked = 1;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("Door " + targetDoor.Name + " is locked", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}

		private void unlocked(GameClient client, GameDoor targetDoor)
		{
			targetDoor.Locked = 0;
			targetDoor.SaveIntoDatabase();
			client.Out.SendMessage("Door " + targetDoor.Name + " is unlocked", EChatType.CT_System, EChatLoc.CL_SystemWindow);
		}


		private void kill(GameClient client, GameDoor targetDoor, string[] args)
		{
			try
			{
				lock (targetDoor.XPGainers.SyncRoot)
				{
					targetDoor.attackComponent.AddAttacker(client.Player);
					targetDoor.AddXPGainer(client.Player, targetDoor.Health);
					targetDoor.Die(client.Player);
					targetDoor.XPGainers.Clear();
					client.Out.SendMessage("Door " + targetDoor.Name + " health reaches 0", EChatType.CT_System,
										   EChatLoc.CL_SystemWindow);
				}
			}
			catch (Exception e)
			{
				client.Out.SendMessage(e.ToString(), EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}

		private string CheckName(string name, GameClient client)
		{
			if (name.Length > 47)
				client.Out.SendMessage("The door name must not be longer than 47 bytes", EChatType.CT_System,
				                       EChatLoc.CL_SystemWindow);
			return name;
		}

		private string CheckGuildName(string name, GameClient client)
		{
			if (name.Length > 47)
				client.Out.SendMessage("The guild name is " + name.Length + ", but only 47 bytes 'll be displayed",
				                       EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return name;
		}
	}
}
