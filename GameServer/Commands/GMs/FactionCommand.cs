using System;
using System.Collections;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Languages;

namespace Core.GS.Commands
{
	[Command(
	   "&faction",
	   EPrivLevel.GM,
	   "GMCommands.Faction.Description",
	   "GMCommands.Faction.Usage.Create",
	   "GMCommands.Faction.Usage.Assign",
	   "GMCommands.Faction.Usage.AddFriend",
	   "GMCommands.Faction.Usage.AddEnemy",
	   "GMCommands.Faction.Usage.List",
	   "GMCommands.Faction.Usage.Select")]
	public class FactionCommand : ACommandHandler, ICommandHandler
	{
		protected string TEMP_FACTION_LAST = "TEMP_FACTION_LAST";

		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				DisplaySyntax(client);
				return;
			}
			FactionUtil myfaction = client.Player.TempProperties.GetProperty<FactionUtil>(TEMP_FACTION_LAST, null);
			switch (args[1])
			{
				#region Create
				case "create":
					{
						if (args.Length < 4)
						{
							DisplaySyntax(client);
							return;
						}
						string name = args[2];
						int baseAggro = 0;
						try
						{
							baseAggro = Convert.ToInt32(args[3]);
						}
						catch
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Create.BAMustBeNumber"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}

						int max = 0;
						//Log.Info("count:" + FactionMgr.Factions.Count.ToString());
						if (FactionMgr.Factions.Count != 0)
						{
							//Log.Info("count >0");
							IEnumerator enumerator = FactionMgr.Factions.Keys.GetEnumerator();
							while (enumerator.MoveNext())
							{
								//Log.Info("max :" + max + " et current :" + (int)enumerator.Current);
								max = System.Math.Max(max, (int)enumerator.Current);
							}
						}
						//Log.Info("max :" + max);
						DbFaction dbfaction = new DbFaction();
						dbfaction.BaseAggroLevel = baseAggro;
						dbfaction.Name = name;
						dbfaction.ID = (max + 1);
						//Log.Info("add obj to db with id :" + dbfaction.ID);
						GameServer.Database.AddObject(dbfaction);
						//Log.Info("add obj to db");
						myfaction = new FactionUtil();
						myfaction.LoadFromDatabase(dbfaction);
						FactionMgr.Factions.Add(dbfaction.ID, myfaction);
						client.Player.TempProperties.SetProperty(TEMP_FACTION_LAST, myfaction);
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Create.NewCreated"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
					}
					break;
				#endregion Create
				#region Assign
				case "assign":
					{
						if (myfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}

						GameNpc npc = client.Player.TargetObject as GameNpc;
						if (npc == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Assign.MustSelectMob"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						npc.Faction = myfaction;
						client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.Assign.MobHasJoinedFact", npc.Name, myfaction.Name), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
					}
					break;
				#endregion Assign
				#region AddFriend
				case "addfriend":
					{
						if (myfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						int id = 0;
						try
						{
							id = Convert.ToInt32(args[2]);
						}
						catch
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						FactionUtil linkedfaction = FactionMgr.GetFactionByID(id);
						if (linkedfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						DbFactionLinks dblinkedfaction = new DbFactionLinks();
						dblinkedfaction.FactionID = myfaction.Id;
						dblinkedfaction.LinkedFactionID = linkedfaction.Id;
						dblinkedfaction.IsFriend = true;
						GameServer.Database.AddObject(dblinkedfaction);
						myfaction.AddFriendFaction(linkedfaction);
					}
					break;
				#endregion AddFriend
				#region AddEnemy
				case "addenemy":
					{
						if (myfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.MustSelectFaction"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						int id = 0;
						try
						{
							id = Convert.ToInt32(args[2]);
						}
						catch
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						FactionUtil linkedfaction = FactionMgr.GetFactionByID(id);
						if (linkedfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						DbFactionLinks dblinkedfaction = new DbFactionLinks();
						dblinkedfaction.FactionID = myfaction.Id;
						dblinkedfaction.LinkedFactionID = linkedfaction.Id;
						dblinkedfaction.IsFriend = false;
						GameServer.Database.AddObject(dblinkedfaction);
						myfaction.EnemyFactions.Add(linkedfaction);
					}
					break;
				#endregion AddEnemy
				#region List
				case "list":
					{
						foreach (FactionUtil faction in FactionMgr.Factions.Values)
							client.Player.Out.SendMessage("#" + faction.Id.ToString() + ": " + faction.Name + " (" + faction._baseAggroLevel + ")", EChatType.CT_Say, EChatLoc.CL_SystemWindow);
						return;
					}
				#endregion List
				#region Select
				case "select":
					{
						if (args.Length < 3)
						{
							DisplaySyntax(client);
							return;
						}
						int id = 0;
						try
						{
							id = Convert.ToInt32(args[2]);
						}
						catch
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.IndexMustBeNumber"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						FactionUtil tempfaction = FactionMgr.GetFactionByID(id);
						if (tempfaction == null)
						{
							client.Player.Out.SendMessage(LanguageMgr.GetTranslation(client.Account.Language, "GMCommands.Faction.FactionNotLoaded"), EChatType.CT_Say, EChatLoc.CL_SystemWindow);
							return;
						}
						client.Player.TempProperties.SetProperty(TEMP_FACTION_LAST, tempfaction);
					}
					break;
				#endregion Select
				#region Default
				default:
					{
						DisplaySyntax(client);
						return;
					}
				#endregion Default
			}
		}
	}
}
