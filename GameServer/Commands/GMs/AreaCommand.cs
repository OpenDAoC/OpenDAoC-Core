using System;
using System.Collections.Generic;
using System.Reflection;
using DOL.Database;
using DOL.GS.Keeps;
using DOL.Language;

namespace DOL.GS.Commands;

[Command(
	"&area",
	EPrivLevel.GM,
	"GMCommands.Area.Description",
	"GMCommands.Area.Usage.Create")]
public class AreaCommand : ACommandHandler, ICommandHandler
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

			case "create":
			{
				if (args.Length != 7)
				{
					DisplaySyntax(client);
					return;
				}

				DbArea area = new DbArea();
				area.Description = args[2];

				switch (args[3].ToLower())
				{
					case "circle":
						area.ClassType = "DOL.GS.Area+Circle";
						break;
					case "square":
						area.ClassType = "DOL.GS.Area+Square";
						break;
					case "safe":
					case "safearea":
						area.ClassType = "DOL.GS.Area+SafeArea";
						break;
					case "bind":
					case "bindarea":
						area.ClassType = "DOL.GS.Area+BindArea";
						break;
					default:
					{
						DisplaySyntax(client);
						return;
					}
				}

				area.Radius = Convert.ToInt16(args[4]);
				switch (args[5].ToLower())
				{
					case "y":
					{
						area.CanBroadcast = true;
						break;
					}
					case "n":
					{
						area.CanBroadcast = false;
						break;
					}
					default:
					{
						DisplaySyntax(client);
						return;
					}
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
				DisplayMessage(client,
					LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Area.AreaCreated",
						area.Description, area.X, area.Z, area.Radius, area.CanBroadcast.ToString(), area.Sound));
				break;
			}

			#endregion Create

			case "info":
			{
				string name = "(Area Info)";
				var info = new List<string>();
				//info.Add("        Current Areas : " + client.Player.CurrentAreas);
				//info.Add(" ");
				var areas = client.Player.CurrentAreas;
				foreach (var area in areas)
				{
					info.Add("Area ClassType: " + area.GetType());
					info.Add(" ");
					info.Add("Area ToString: " + area.ToString());
					info.Add(" ");

					if (area is KeepArea ka)
					{
						info.Add("Area Keep: " + ka.Keep.Name);
						info.Add(" ");

						foreach (var guard in ka.Keep.Guards.Values)
						{
							info.Add("Area Guard: " + guard);
							info.Add(" ");
						}

						foreach (var component in ka.Keep.KeepComponents)
						{
							info.Add("Area Component: " + component);
							info.Add(" ");
						}

					}


				}

				client.Out.SendCustomTextWindow("[ " + name + " ]", info);
				break;
			}

			#region Default

			default:
			{
				DisplaySyntax(client);
				break;
			}

			#endregion Default
		}
	}
}