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
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Handles all user-based interaction for the '/reload' command
	/// </summary>
	[CmdAttribute(
		// Enter '/reload' to list all associated subcommands
		"&reload",
		// Message: '/reload' - Removes and then re-adds specific in-game elements. This is used to apply any recent database changes to the live server.
		"AdminCommands.Reload.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Removes and then re-adds specific in-game elements. This is used to apply any recent database changes to the live server.
		"AdminCommands.Reload.Description",
		// Syntax: /reload mob
		"AdminCommands.Reload.Syntax.Mob",
		// Message: Reloads all in-game objects in your current region with the 'GameNPC' type.
		"AdminCommands.Reload.Usage.Mob",
		// Syntax: /reload mob model <modelID>
		"AdminCommands.Reload.Syntax.ModelMob",
		// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and the model ID specified.
		"AdminCommands.Reload.Usage.ModelMob",
		// Syntax: /reload mob name <mobName>
		"AdminCommands.Reload.Syntax.NameMob",
		// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the name specified.
		"AdminCommands.Reload.Usage.NameMob",
		// Syntax: /reload mob realm <realm>
		"AdminCommands.Reload.Syntax.RealmMob",
		// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the realm specified.
		"AdminCommands.Reload.Usage.RealmMob",
		// Syntax: /reload object [all|model|name|realm]
		"AdminCommands.Reload.Syntax.Object",
		// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type.
		"AdminCommands.Reload.Usage.Object",
		// Syntax: /reload object model <modelID>
		"AdminCommands.Reload.Syntax.ModelObject",
		// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type and the model ID specified.
		"AdminCommands.Reload.Usage.ModelObject",
		// Syntax: /reload object name <objName>
		"AdminCommands.Reload.Syntax.NameObject",
		// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type and matching the name specified.
		"AdminCommands.Reload.Usage.NameObject",
		// Syntax: /reload object realm <realm>
		"AdminCommands.Reload.Syntax.RealmObject",
		// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type and matching the realm specified.
		"AdminCommands.Reload.Usage.RealmObject",
		// Syntax: /reload npctemplates
		"AdminCommands.Reload.Syntax.NPCTemplates",
		// Message: Reloads all database objects from the 'NpcTemplate' table.
		"AdminCommands.Reload.Usage.NPCTemplates",
		// Message: /reload realm
		"AdminCommands.Reload.Syntax.Realm",
		// Message: Displays all accepted values for '/reload mob realm' and '/reload object realm' subcommands.
		"AdminCommands.Reload.Usage.Realm",
		// Syntax: /reload serverproperties
		"AdminCommands.Reload.Syntax.Serverproperties",
		// Message: Reloads the server cache with any changes from the 'serverproperty' table.
		"AdminCommands.Reload.Usage.Serverproperties",
		// Syntax: /reload specs
		"AdminCommands.Reload.Syntax.Specs",
		// Message: Reloads all database objects from the 'Specialization' table.
		"AdminCommands.Reload.Usage.Specs",
		// Syntax: /reload spells
		"AdminCommands.Reload.Syntax.Spells",
		// Message: Reloads all database objects from the 'Spell' table.
		"AdminCommands.Reload.Usage.Spells",
		// Syntax: /reload teleports
		"AdminCommands.Reload.Syntax.Teleports",
		// Message: Reloads all database objects from the 'Teleport' table.
		"AdminCommands.Reload.Usage.Teleports")]
	public class ReloadCommandHandler : AbstractCommandHandler, ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void OnCommand(GameClient client, string[] args)
		{
			if (client.Player == null) return;
			
			ushort region = client.Player.CurrentRegionID;
			string arg = "";
			int argLength = args.Length - 1;

			if (argLength < 1)
			{
				DisplaySyntax(client);
				return;
			}
			
			if (args[1] == "mob" && args.Length == 1)
			{
				// Message: <----- '/{0}{1}' Subcommand {3}----->
				// Message: Use the following syntax for this command:
				// Syntax: /reload mob [all|model|name|realm]
				// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and the filter specified.
				DisplayHeadSyntax(client, "reload", "mob", "", 3, false, "AdminCommands.Reload.Syntax.Mob", "AdminCommands.Reload.Usage.Mob");
				return;
			}
			
			switch (args[1].ToLower())
			{
				case "mob":
				{
					if (args.Length == 1)
						ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AdminCommands.Reload.Header.Realms", null);
					if (args.Length >= 2)
					{
						if (args[2] == "all")
						{
							arg = "all";
							ReloadMobs(client, region, arg, arg);
							return;
						}
						if (args[2] == "model")
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob model <modelID>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and the model ID specified.
								DisplayHeadSyntax(client, "reload", "mob", "model", 3, false, "AdminCommands.Reload.Syntax.ModelMob", "AdminCommands.Reload.Usage.ModelMob");
								return;
							}
							arg = args[3];
							ReloadMobs(client, region, args[2], arg);
							return;
						}
						if (args[2] == "name")
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob name <mobName>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the name specified.
								DisplayHeadSyntax(client, "reload", "mob", "name", 3, false, "AdminCommands.Reload.Syntax.NameMob", "AdminCommands.Reload.Usage.NameMob");
								return;
							}
								
							arg = String.Join(" ", args, 3, args.Length - 3);
							ReloadMobs(client, region, args[2], arg);
							return;
						}
						if (args[2] == "realm")
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob realm <realm>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the realm specified.
								DisplayHeadSyntax(client, "reload", "mob", "realm", 3, false, "AdminCommands.Reload.Syntax.RealmMob", "AdminCommands.Reload.Usage.RealmMob");
								return;
							}
								
							if (args[3] == "0" || args[3] == "none" || args[3] == "no" || args[3] == "n")
								arg = "None";
							else if (args[3] == "1" || args[3] == "a" || args[3] == "alb" || args[3] == "albion")
								arg = "Albion";
							else if (args[3] == "2" || args[3] == "m" || args[3] == "mid" || args[3] == "midgard")
								arg = "Midgard";
							else if (args[3] == "3" || args[3] == "h" || args[3] == "hib" || args[3] == "hibernia")
								arg = "Hibernia";
							else
							{
								// Message: <----- '/{0}' Command {1}----->
								// Message: <----- '/{0}{1}' Subcommand {2}----->
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: It is recommended that you perform actions associated with this command with the Atlas Admin (https://admin.atlasfreeshard.com). Otherwise, use the following syntax:
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob realm <realm>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the realm specified.
								DisplayHeadSyntax(client, "reload", "mob", "realm", 3, false, "AdminCommands.Reload.Syntax.RealmMob", "AdminCommands.Reload.Usage.RealmMob");
								return;
							}
								
							ReloadMobs(client, region, args[2], arg);
							return;
						}
					}
					break;
				}
				case "npctemplates":
				{
					NpcTemplateMgr.Reload();
					
					// Message: Reload complete! All NPC Templates have been added to the live cache.
					ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.NPCTempReloaded", null);
					
					log.Info("[SUCCESS] - NPC templates reloaded.");
					return;
				}
				case "object":
				{
					switch (args[2])
					{
						case "all":
						case "":
						{
							arg = "all";
							ReloadStaticItem(client, region, arg, arg);
							return;
						}
						case "model":
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload object model <modelID>
								// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type and the model ID specified.
								DisplayHeadSyntax(client, "reload", "mob", "model", 3, false, "AdminCommands.Reload.Syntax.ModelObject", "AdminCommands.Reload.Usage.ModelObject");
								return;
							}
							arg = args[3];
							ReloadStaticItem(client, region, args[2], arg);
							return;
						}
						case "name":
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload object name <objName>
								// Message: Reloads all in-game objects in your current region with the 'GameStaticItem' type and matching the name specified.
								DisplayHeadSyntax(client, "reload", "object", "name", 3, false, "AdminCommands.Reload.Syntax.NameObject", "AdminCommands.Reload.Usage.NameObject");
								return;
							}
							
							arg = String.Join(" ", args, 3, args.Length - 3);
							ReloadStaticItem(client, region, args[2], arg);
							return;
						}
						case "realm":
						{
							if (argLength == 2)
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob realm <realm>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the realm specified.
								DisplayHeadSyntax(client, "reload", "object", "realm", 3, false, "AdminCommands.Reload.Syntax.RealmMob", "AdminCommands.Reload.Usage.RealmMob");
								return;
							}
							
							if (args[3] == "0" || args[3] == "None" || args[3] == "none" || args[3] == "no" || args[3] == "n")
								arg = "None";
							else if (args[3] == "1" || args[3] == "a" || args[3] == "alb" || args[3] == "Alb" || args[3] == "albion" || args[3] == "Albion")
								arg = "Albion";
							else if (args[3] == "2" || args[3] == "m" || args[3] == "mid" || args[3] == "Mid" || args[3] == "midgard" || args[3] == "Midgard")
								arg = "Midgard";
							else if (args[3] == "3" || args[3] == "h" || args[3] == "hib" || args[3] == "Hib" || args[3] == "hibernia" || args[3] == "Hibernia")
								arg = "Hibernia";
							else
							{
								// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
								// Message: Use the following syntax for this command:
								// Syntax: /reload mob realm <realm>
								// Message: Reloads all in-game objects in your current region with the 'GameNPC' type and matching the realm specified.
								DisplayHeadSyntax(client, "reload", "object", "realm", 3, false, "AdminCommands.Reload.Syntax.RealmMob", "AdminCommands.Reload.Usage.RealmMob");
								return;
							}
							
							ReloadStaticItem(client, region, args[2], arg);
							return;
						}
					}
					break;
				}
				case "realm":
				{
					// Message: <----- Realm Syntax ----->
					ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AdminCommands.Reload.Header.Realms", null);
					// Message: None = 0, n, no, none
					ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Reload.Usage.NoneRealm", null);
					// Message: Albion = 1, a, alb, albion
					ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Reload.Usage.AlbRealm", null);
					// Message: Midgard = 2, m, mid, midgard
					ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Reload.Usage.MidRealm", null);
					// Message: Hibernia = 3, h, hib, hibernia
					ChatUtil.SendTypeMessage(eMsg.Command, client, "AdminCommands.Reload.Usage.HibRealm", null);
					return;
				}
				case "specs":
				{
					int count = SkillBase.LoadSpecializations();
					// Message: Reload complete! {0} specializations have been added to the live cache.
					ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.SpecsReloaded", count);
					log.Info(string.Format("[SUCCESS] - Reloaded DB and {0} specializations loaded!", count));
					return;
				}
				case "spells":
				{
					SkillBase.ReloadDBSpells();
					int loaded = SkillBase.ReloadSpellLines();
					// Message: Reload complete! {0} spells from all spell lines have been added to the live cache.
					ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.SpellsReloaded", loaded);
					log.Info(string.Format("[SUCCESS] - Reloaded DB and {0} spells for all spell lines!", loaded));
					return;
				}
				case "teleports":
				{
					WorldMgr.LoadTeleports();
					// Message: Reload complete! All teleport locations have been added to the live cache.
					ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.TeleportsReloaded", null);
					log.Info("[SUCCESS] - Teleport locations reloaded.");
					return;
				}
				case "serverproperties":
				{
					// Dated code for people still using XML setups instead of MySQL
					if (GameServer.Instance.Configuration.DBType == DOL.Database.Connection.ConnectionType.DATABASE_XML)
					{
						// Message: XML values are cached and cannot be updated. Server properties may only be reloaded using MySQL.
						ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Err.XMLOld", null);
						return;
					}
			
					ServerProperties.Properties.Reload();
					
					// Message: Values from the 'serverproperty' table have been reloaded into the server's cache!
					ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.PropsRefreshed", null);
					return;
				}
				default:
				{
					// Lists all '/reload' type subcommand syntax (see section above)
					DisplaySyntax(client);
					break;
				}
			}
			
		}

		private void ReloadMobs(GameClient client, ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("[ERROR] - Region reload not supported from console.");
				return;
			}

			// Message: Removing and then re-adding all mobs where {0} = {1}.
			ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.RemovingReaddingMobs", arg1, arg2);

			int added = 0;
			int removed = 0;

			foreach (GameNPC mob in WorldMgr.GetNPCsFromRegion(region))
			{
				if (!mob.LoadedFromScript)
				{
					switch (arg1)
					{
						case "all":
						{
							mob.RemoveFromWorld();
							removed++;
							Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
							
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								added++;
							}
							break;
						}
						case "model":
						{
							if (mob.Model == Convert.ToUInt16(arg2))
							{
								mob.RemoveFromWorld();
								removed++;
								WorldObject mobs = GameServer.Database.FindObjectByKey<WorldObject>(mob.InternalID);
								
								if (mobs != null)
								{
									mob.LoadFromDatabase(mobs);
									mob.AddToWorld();
									added++;
								}
							}
							break;
						}
						case "name":
						{
							if (mob.Name == arg2)
							{
								mob.RemoveFromWorld();
								removed++;
								Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);

								if (mobs != null)
								{
									mob.LoadFromDatabase(mobs);
									mob.AddToWorld();
									added++;
								}
							}
							break;
						}
						case "realm":
						{
							eRealm realm = eRealm.None;
							if (arg2 == "None") realm = eRealm.None;
							if (arg2 == "Albion") realm = eRealm.Albion;
							if (arg2 == "Midgard") realm = eRealm.Midgard;
							if (arg2 == "Hibernia") realm = eRealm.Hibernia;

							if (mob.Realm == realm)
							{
								mob.RemoveFromWorld();
								removed++;
								Mob mobs = GameServer.Database.FindObjectByKey<Mob>(mob.InternalID);
								
								if (mobs != null)
								{
									mob.LoadFromDatabase(mobs);
									mob.AddToWorld();
									added++;
								}
							}
							break;
						}
					}
				}
			}

			// Message: Reload complete! {0} mobs were removed and {1} added to this region.
			ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.TotalMobsRemovedAdded", removed, added);
		}

		private void ReloadStaticItem(GameClient client, ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("[ERROR] - Region reload not supported from console.");
				return;
			}
			
			// Message: Removing and then re-adding all objects where {0} = {1}.
			ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.RemovingReaddingObjs", arg1, arg2);

			int added = 0;
			int removed = 0;

			foreach (GameStaticItem staticItem in WorldMgr.GetStaticItemFromRegion(region))
			{
				if (!staticItem.LoadedFromScript)
				{
					switch (arg1)
					{
						case "all":
						{
							staticItem.RemoveFromWorld();
							removed++;
							WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
							
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
								added++;
							}
							break;
						}
						case "model":
						{
							if (staticItem.Model == Convert.ToUInt16(arg2))
							{
								staticItem.RemoveFromWorld();

								WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
								if (obj != null)
								{
									staticItem.LoadFromDatabase(obj);
									staticItem.AddToWorld();
									added++;
								}
							}
							break;
						}
						case "name":
						{
							if (staticItem.Name == arg2)
							{
								staticItem.RemoveFromWorld();
								removed++;
								WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
								
								if (obj != null)
								{
									staticItem.LoadFromDatabase(obj);
									staticItem.AddToWorld();
									added++;
								}
							}
							break;
						}
						case "realm":
						{
							eRealm realm = eRealm.None;
							if (arg2 == "None") realm = eRealm.None;
							if (arg2 == "Albion") realm = eRealm.Albion;
							if (arg2 == "Midgard") realm = eRealm.Midgard;
							if (arg2 == "Hibernia") realm = eRealm.Hibernia;

							if (staticItem.Realm == realm)
							{
								staticItem.RemoveFromWorld();
								removed++;
								WorldObject obj = GameServer.Database.FindObjectByKey<WorldObject>(staticItem.InternalID);
								
								if (obj != null)
								{
									staticItem.LoadFromDatabase(obj);
									staticItem.AddToWorld();
									added++;
								}
							}
							break;
						}
					}
				}
			}
			
			// Message: Reload complete! {0} objects were removed and {1} added to this region.
			ChatUtil.SendTypeMessage(eMsg.Debug, client, "AdminCommands.Reload.Msg.TotalObjRemovedAdded", removed, added);
		}
	}
}
