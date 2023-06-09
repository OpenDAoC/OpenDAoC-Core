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
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles area creation from in-game.
	/// </summary>
	[CmdAttribute(
		"&area",
		// Message: '/area' - Creates areas and displays area information in-game.
		"GMCommands.Area.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.GM,
		// Message: Allows for the management of areas and area information.
		"GMCommands.Area.Description",
		// Message: /area create <name> < circle | square | safe | bind > <radius> <broadcast ( y | n )> <soundID>
		"GMCommands.Area.Syntax.Create",
		// Message: Creates an area of the specified type/shape and radius.
		"GMCommands.Area.Usage.Create",
		// Message: /area info
		"GMCommands.Area.Syntax.Info",
		// Message: Displays relevant information regarding the areas in which the client are located.
		"GMCommands.Area.Usage.Info"
	)]
	public class AreaCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length == 1)
			{
				DisplaySyntax(client);
				return;
			}

			switch (args[1].ToLower())
			{
				#region Create
				// --------------------------------------------------------------------------------
				// CREATE (Admin/GM command)
				// '/area create <name> < circle | square | safe | bind > <radius> <broadcast ( y | n )> <soundID>'
				// Creates an area of the specified type/shape and radius.
				// --------------------------------------------------------------------------------
				case "create":
					{
						if (args.Length != 7)
						{
							DisplaySyntax(client);
							return;
						}

						DBArea area = new DBArea();
						area.Description = args[2];

						switch (args[3].ToLower())
						{
							case "circle": area.ClassType = "DOL.GS.Area+Circle"; break;
							case "square": area.ClassType = "DOL.GS.Area+Square"; break;
							case "safe":
							case "safearea": area.ClassType = "DOL.GS.Area+SafeArea"; break;
							case "bind":
							case "bindarea": area.ClassType = "DOL.GS.Area+BindArea"; break;
							default:
								{
									DisplaySyntax(client);
									return;
								}
						}

						area.Radius = Convert.ToInt16(args[4]);

						switch (args[5].ToLower())
						{
							case "y": { area.CanBroadcast = true; break; }
							case "n": { area.CanBroadcast = false; break; }
							default: { DisplaySyntax(client); return; }
						}

						area.Sound = byte.Parse(args[6]);
						area.Region = client.Player.CurrentRegionID;
						area.X = client.Player.X;
						area.Y = client.Player.Y;
						area.Z = client.Player.Z;

						Assembly gasm = Assembly.GetAssembly(typeof(GameServer));
						AbstractArea newArea = (AbstractArea)gasm.CreateInstance(area.ClassType, false);

						newArea.LoadFromDatabase(area);

						newArea.Sound = area.Sound;
						newArea.CanBroadcast = area.CanBroadcast;

						WorldMgr.GetRegion(client.Player.CurrentRegionID).AddArea(newArea);
						GameServer.Database.AddObject(area);

						// Message: [SUCCESS] You have created a new area!
						ChatUtil.SendTypeMessage((int)eMsg.Important, client, "GMCommands.Area.Msg.AreaCreated", null);
						// Message: [INFO] Description:
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaDesc", null);
						// Message: --- Name: {0}
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaName", area.Description);
						// Message: --- X: {0}, Y: {1}, Z: {2}, Region: {3}
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaLoc", area.X, area.Y, area.Z, area.Region);
						// Message: --- Radius: {0}
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaRadius", area.Radius);
						// Message: --- Broadcast: {0}
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaBroad", area.CanBroadcast.ToString());
						// Message: --- Sound: {0}
						ChatUtil.SendTypeMessage((int)eMsg.Debug, client, "GMCommands.Area.Msg.AreaSound", area.Sound);

						break;
					}
				#endregion Create
				#region Info
				// --------------------------------------------------------------------------------
				// INFO (Admin/GM command)
				// '/area info'
				// Displays relevant information regarding the areas in which the client are located.
				// --------------------------------------------------------------------------------
				case "info":
                    {
						string name = "Area Information";
						var info = new List<string>();
						//info.Add("        Current Areas : " + client.Player.CurrentAreas);
						//info.Add(" ");
						var areas = client.Player.CurrentAreas;

                        foreach (var area in areas)
                        {
	                        info.Add(" ");
							info.Add("Area Class Type: " + area.GetType());
							info.Add(" ");
							info.Add("Area String: " + area.ToString());
							info.Add(" ");

							if (area is KeepArea ka)
                            {
								info.Add("Area Keep: " + ka.Keep.Name);
								info.Add(" ");

                                foreach (var guard in ka.Keep.Guards.Values)
                                {
									info.Add("Keep Guard: " + guard);
									info.Add(" ");
								}

                                foreach (var component in ka.Keep.KeepComponents)
                                {
									info.Add("Keep Component: " + component);
									info.Add(" ");
                                }
                            }

							info.Add("AllCommands.Header.Note.Dashes");
                        }

                        ChatUtil.SendWindowMessage((int)eWindow.Text, client, name, info);

						break;
                    }
				#endregion Info
				#region Default
				// --------------------------------------------------------------------------------
				// DEFAULT
				// Triggered whenever the command is incorrectly entered.
				// --------------------------------------------------------------------------------
				default:
					{
						DisplaySyntax(client);
						break;
					}
				#endregion Default
			}
		}
	}
}