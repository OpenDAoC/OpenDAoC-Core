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
using System.Collections;
using DOL.Database;
using DOL.GS.Movement;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands
{
	[CmdAttribute(
		"&path",
		ePrivLevel.GM,
		"There are several path functions",
		"/path create - creates a new temporary path, deleting any existing temporary path",
		"/path load <pathname> - loads a path from db",
		"/path add [speedlimit] [wait time in second] - adds a point at the end of the current path",
		"/path save <pathname> - saves a path to db",
		"/path travel - makes a target npc travel the current path",
		"/path stop - clears the path for a targeted npc and tells npc to walk to spawn",
		"/path speed [speedlimit] - sets the speed of all path nodes",
		"/path assigntaxiroute <Destination> - sets the current path as taxiroute on stablemaster",
		"/path hide - hides all path markers but does not delete the path",
		"/path delete - deletes the temporary path",
		"/path type - changes the paths type")]
	public class PathCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		protected string TEMP_PATH_FIRST = "TEMP_PATH_FIRST";
		protected string TEMP_PATH_LAST = "TEMP_PATH_LAST";
		protected string TEMP_PATH_OBJS = "TEMP_PATH_OBJS";

		private void CreatePathPointObject(GameClient client, PathPoint pp, int id)
		{
			//Create a new object
			GameStaticItem obj = new GameStaticItem();
			//Fill the object variables
			obj.X = pp.X;
			obj.Y = pp.Y;
			obj.Z = pp.Z + 1; // raise a bit off of ground level
			obj.CurrentRegion = client.Player.CurrentRegion;
			obj.Heading = client.Player.Heading;
			obj.Name = $"PP ({id})";
			obj.Model = 488;
			obj.Emblem = 0;
			obj.AddToWorld();

			ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS, null);
			if (objs == null)
				objs = new ArrayList();
			objs.Add(obj);
			client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, objs);
		}

		private void RemoveAllPathPointObjects(GameClient client)
		{
			ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS, null);
			if (objs == null)
				return;

			// remove the markers
			foreach (GameStaticItem obj in objs)
				obj.Delete();

			// clear the path point array
			objs.Clear();

			// remove all path properties
			client.Player.TempProperties.SetProperty(TEMP_PATH_OBJS, null);
			client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, null);
			client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, null);
		}

		private void PathHide(GameClient client)
		{
			ArrayList objs = client.Player.TempProperties.GetProperty<ArrayList>(TEMP_PATH_OBJS, null);
			if (objs == null)
				return;

			// remove the markers
			foreach (GameStaticItem obj in objs)
				obj.Delete();
		}

		private void PathCreate(GameClient client)
		{
			//Remove old temp objects
			RemoveAllPathPointObjects(client);

			PathPoint startpoint = new PathPoint(client.Player.X, client.Player.Y, client.Player.Z, 1000, EPathType.Once);
			client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, startpoint);
			client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, startpoint);
			client.Player.Out.SendMessage("Path creation started! You can add new pathpoints via /path add now!", eChatType.CT_System, eChatLoc.CL_SystemWindow);
			CreatePathPointObject(client, startpoint, 1);
		}

		private void PathAdd(GameClient client, string[] args)
		{
			PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
			if (path == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create first!");
				return;
			}

			short speedlimit = 1000;
			int waittime = 0;
			if (args.Length > 2)
			{
				try
				{
					speedlimit = short.Parse(args[2]);
				}
				catch
				{
					DisplayMessage(client, "No valid speedlimit '{0}'!", args[2]);
					return;
				}

				if (args.Length > 3)
				{
					try
					{
						waittime = int.Parse(args[3]);
					}
					catch
					{
						DisplayMessage(client, "No valid wait time '{0}'!", args[3]);
					}
				}
			}

			PathPoint newpp = new PathPoint(client.Player.X, client.Player.Y, client.Player.Z, speedlimit, path.Type);
			newpp.WaitTime = waittime * 10;
			path.Next = newpp;
			newpp.Prev = path;
			client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, newpp);

			int len = 0;
			while (path.Prev != null)
			{
				len++;
				path = path.Prev;
			}
			len += 2;
			CreatePathPointObject(client, newpp, len);
			DisplayMessage(client, "Pathpoint added. Current pathlength = {0}", len);
		}

		private void PathSpeed(GameClient client, string[] args)
		{
			if (args.Length < 3)
			{
				DisplayMessage(client, "No valid speedlimit '{0}'!", args[2]);
				return;
			}

			short speedlimit;
			try
			{
				speedlimit = short.Parse(args[2]);
			}
			catch
			{
				DisplayMessage(client, "No valid speedlimit '{0}'!", args[2]);
				return;
			}

			PathPoint pathpoint = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST, null);

			if (pathpoint == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create first!");
				return;
			}

			pathpoint.MaxSpeed = speedlimit;

			while (pathpoint.Next != null)
			{
				pathpoint = pathpoint.Next;
				pathpoint.MaxSpeed = speedlimit;
			}

			DisplayMessage(client, "All path points set to speed {0}!", args[2]);
		}

		private void PathTravel(GameClient client)
		{
			PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
			if (client.Player.TargetObject == null || !(client.Player.TargetObject is GameNPC))
			{
				DisplayMessage(client, "You need to select a mob first!");
				return;
			}

			if (path == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create first!");
				return;
			}
			short speed = Math.Min(((GameNPC)client.Player.TargetObject).MaxSpeedBase, path.MaxSpeed);

			// clear any current path
			((GameNPC)client.Player.TargetObject).CurrentWaypoint = null;

			// set the new path
			((GameNPC)client.Player.TargetObject).CurrentWaypoint = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_FIRST, null);

			((GameNPC)client.Player.TargetObject).MoveOnPath(speed);

			DisplayMessage(client, "{0} told to travel path!", client.Player.TargetObject.Name);

		}

		private void PathStop(GameClient client)
		{
			if (client.Player.TargetObject == null || !(client.Player.TargetObject is GameNPC))
			{
				DisplayMessage(client, "You need to select a mob first!");
				return;
			}

			// clear any current path
			GameNPC npcTarget = (GameNPC) client.Player.TargetObject;
			npcTarget.CurrentWaypoint = null;
			npcTarget.ReturnToSpawnPoint(npcTarget.MaxSpeed);

			DisplayMessage(client, "{0} told to walk to spawn!", client.Player.TargetObject.Name);
		}

		private void PathType(GameClient client, string[] args)
		{
			PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
			if (args.Length < 2)
			{
				DisplayMessage(client, "Usage: /path type <pathtype>");
				DisplayMessage(client, "Current path type is '{0}'", path.Type.ToString());
				DisplayMessage(client, "Possible pathtype values are:");
				DisplayMessage(client, String.Join(", ", Enum.GetNames(typeof(EPathType))));
				return;
			}
			if (path == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create or /path load first!");
				return;
			}

			EPathType pathType = EPathType.Once;
			try
			{
				pathType = (EPathType)Enum.Parse(typeof(EPathType), args[2], true);
			}
			catch
			{
				DisplayMessage(client, "Usage: /path type <pathtype>");
				DisplayMessage(client, "Current path type is '{0}'", path.Type.ToString());
				DisplayMessage(client, "PathType must be one of the following:");
				DisplayMessage(client, String.Join(", ", Enum.GetNames(typeof(EPathType))));
				return;
			}

			path.Type = pathType;
			PathPoint temp = path.Prev;
			while ((temp != null) && (temp != path))
			{
				temp.Type = pathType;
				temp = temp.Prev;
			}
			DisplayMessage(client, "Current path type set to '{0}'", path.Type.ToString());
		}

		private void PathLoad(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplayMessage(client, "Usage: /path load <pathname>");
				return;
			}

			string pathName = string.Join(" ", args, 2, args.Length - 2);
			PathPoint pathPoint = MovementMgr.LoadPath(pathName);

			if (pathPoint == null)
			{
				DisplayMessage(client, "Path '{0}' not found!", pathName);
				return;
			}

			RemoveAllPathPointObjects(client);
			DisplayMessage(client, "Path '{0}' loaded.", pathName);
			client.Player.TempProperties.SetProperty(TEMP_PATH_FIRST, pathPoint);
			int len = 0;
			PathPoint lastPathPoint;

			do
			{
				lastPathPoint = pathPoint;
				CreatePathPointObject(client, pathPoint, ++len);
				pathPoint = pathPoint.Next;
			} while (pathPoint != null);

			client.Player.TempProperties.SetProperty(TEMP_PATH_LAST, lastPathPoint);
		}

		private void PathSave(GameClient client, string[] args)
		{
			PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
			if (args.Length < 3)
			{
				DisplayMessage(client, "Usage: /path save <pathname>");
				return;
			}

			if (path == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create first!");
				return;
			}

			string pathname = String.Join(" ", args, 2, args.Length - 2);
			MovementMgr.SavePath(pathname, path);
			DisplayMessage(client, "Path saved as '{0}'", pathname);
		}

		private void PathAssignTaxiRoute(GameClient client, string[] args)
		{
			PathPoint path = client.Player.TempProperties.GetProperty<PathPoint>(TEMP_PATH_LAST, null);
			if (args.Length < 2)
			{
				DisplayMessage(client, "Usage: /path assigntaxiroute <destination>");
				return;
			}

			if (path == null)
			{
				DisplayMessage(client, "No path created yet! Use /path create first!");
				return;
			}

			GameMerchant merchant = null;
			if (client.Player.TargetObject is GameStableMaster)
				merchant = client.Player.TargetObject as GameStableMaster;
			if (client.Player.TargetObject is GameBoatStableMaster)
				merchant = client.Player.TargetObject as GameBoatStableMaster;
			if (merchant == null)
			{
				DisplayMessage(client, "You must select a stable master to assign a taxi route!");
				return;
			}
			string target = String.Join(" ", args, 2, args.Length - 2); ;
			bool ticketFound = false;
			string ticket = "Ticket to " + target;
			// Most //
			// With the new horse system, the stablemasters are using the item.Id_nb to find the horse route in the database
			// So we have to save a path in the database with the Id_nb as a PathID
			// The following string will contain the item Id_nb if it is found in the merchant list
			string pathname = "";
			if (merchant.TradeItems != null)
			{
				foreach (DbItemTemplate template in merchant.TradeItems.GetAllItems().Values)
				{
					if (template != null && template.Name.ToLower() == ticket.ToLower())
					{
						ticketFound = true;
						pathname = template.Id_nb;
						break;
					}

				}
			}
			if (!ticketFound)
			{
				DisplayMessage(client, "Stablemaster has no {0}!", ticket);
				return;
			}
			MovementMgr.SavePath(pathname, path);
			DisplayMessage(client, "Taxi route set to path '{0}'!", pathname);
		}

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1].ToLower())
			{
				case "create":
					{
						PathCreate(client);
						break;
					}
				case "add":
					{
						PathAdd(client, args);
						break;
					}
				case "travel":
					{
						PathTravel(client);
						break;
					}
				case "stop":
					{
						PathStop(client);
						break;
					}
				case "speed":
					{
						PathSpeed(client, args);
						break;
					}
				case "type":
					{
						PathType(client, args);
						break;
					}
				case "save":
					{
						PathSave(client, args);
						break;
					}
				case "load":
					{
						PathLoad(client, args);
						break;
					}
				case "assigntaxiroute":
					{
						PathAssignTaxiRoute(client, args);
						break;
					}
				case "hide":
					{
						PathHide(client);
						break;
					}
				case "delete":
					{
						RemoveAllPathPointObjects(client);
						break;
					}
				default:
					{
						DisplaySyntax(client);
						break;
					}
			}
		}
	}
}
