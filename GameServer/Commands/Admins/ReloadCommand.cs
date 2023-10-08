using System;
using System.Reflection;
using DOL.Database;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Commands
{
	[Command("&Reload",
		EPrivLevel.Admin,
		"Reload various elements",
		"/reload mob|object|specs|spells|teleports"
		)]
	public class ReloadCommand : ICommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private static void SendSystemMessageBase(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage("\n  ===== [[[ Command Reload ]]] ===== \n", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" Reload given element.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageMob(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(" /reload mob ' reload all mob in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload mob ' realm <0/1/2/3>' reload all mob with specifique realm in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload mob ' name <name_you_want>' reload all mob with specifique name in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload mob ' model <model_ID>' reload all mob with specifique model in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageObject(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage(" /reload object ' reload all static object in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload object ' realm <0/1/2/3>' reload all static object with specifique realm in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload object ' name <name_you_want>' reload all static object with specifique name in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" /reload object ' model <model_ID>' reload all static object with specifique model in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageRealm(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage("\n /reload <object/mob> realm <0/1/2/3>' reload all element with specifique realm in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				client.Out.SendMessage(" can use 0/1/2/3 or n/a/m/h or no/alb/mid/hib....", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageName(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage("\n /reload <object/mob>  name <name_you_want>' reload all element with specified name in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}
		private static void SendSystemMessageModel(GameClient client)
		{
			if (client.Player != null)
			{
				client.Out.SendMessage("\n /reload <object/mob>  model <model_ID>' reload all element with specified model_ID in region.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			}
		}

		public void OnCommand(GameClient client, string[] args)
		{
			ushort region = 0;
			if (client.Player != null)
				region = client.Player.CurrentRegionID;
			string arg = "";
			int argLength = args.Length - 1;

			if (argLength < 1)
			{
				if (client.Player != null)
				{
					SendSystemMessageBase(client);
					SendSystemMessageMob(client);
					SendSystemMessageObject(client);
					client.Out.SendMessage(" /reload specs - reload all specializations.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					client.Out.SendMessage(" /reload spells - reload a spells and spelllines, checking db for changed and new spells.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					client.Out.SendMessage(" /reload teleports - reload all teleport locations", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					client.Out.SendMessage(" /reload npctemplates - reload all NPCTemplates", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
				log.Info("/reload command failed, review parameters.");
				return;
			}
			else if (argLength > 1)
			{
				if (args[2] == "realm" || args[2] == "Realm")
				{
					if (argLength == 2)
					{
						SendSystemMessageRealm(client);
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
						SendSystemMessageRealm(client);
						return;
					}
				}
				else if (args[2] == "name" || args[2] == "Name")
				{
					if (argLength == 2)
					{
						SendSystemMessageName(client);
						return;
					}
					arg = String.Join(" ", args, 3, args.Length - 3);
				}
				else if (args[2] == "model" || args[2] == "Model")
				{
					if (argLength == 2)
					{
						SendSystemMessageModel(client);
						return;
					}
					arg = args[3];
				}
			}

			if (args[1] == "mob" || args[1] == "Mob")
			{

				if (argLength == 1)
				{
					arg = "all";
					ReloadMobs(client.Player, region, arg, arg);
				}

				if (argLength > 1)
				{
					ReloadMobs(client.Player, region, args[2], arg);
				}
			}

			if (args[1] == "object" || args[1] == "Object")
			{
				if (argLength == 1)
				{
					arg = "all";
					ReloadStaticItem(region, arg, arg);
				}

				if (argLength > 1)
				{
					ReloadStaticItem(region, args[2], arg);
				}
			}

			if (args[1].ToLower() == "spells")
			{
				SkillBase.ReloadDBSpells();
				int loaded = SkillBase.ReloadSpellLines();
				if (client != null) ChatUtil.SendSystemMessage(client, string.Format("Reloaded db spells and {0} spells for all lines !", loaded));
				log.Info(string.Format("Reloaded db spells and {0} spells for all spell lines !", loaded));
				return;
			}

			if (args[1].ToLower() == "specs")
			{
				int count = SkillBase.LoadSpecializations();
				if (client != null) client.Out.SendMessage(string.Format("{0} specializations loaded.", count), EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				log.Info(string.Format("{0} specializations loaded.", count));
				return;
			}
			
			if (args[1].ToLower() == "teleports")
			{
				WorldMgr.LoadTeleports();
				if (client != null) client.Out.SendMessage("Teleport locations reloaded.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				log.Info("Teleport locations reloaded.");
				return;
			}
			
			if (args[1].ToLower() == "npctemplates")
			{
				NpcTemplateMgr.Reload();
				if (client != null) client.Out.SendMessage("NPC templates reloaded.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				log.Info("NPC templates reloaded.");
				return;
			}
			
			if (args[1].ToLower() == "doors")
			{
				DoorMgr.Init();
				if (client != null) client.Out.SendMessage("Doors reloaded.", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				log.Info("Doors reloaded.");
				return;
			}

			return;
		}

		private void ReloadMobs(GamePlayer player, ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("Region reload not supported from console.");
				return;
			}

			ChatUtil.SendSystemMessage(player, "Reloading Mobs:  " + arg1 + ", " + arg2 + " ...");

			int count = 0;

			foreach (GameNPC mob in WorldMgr.GetNPCsFromRegion(region))
			{
				if (!mob.LoadedFromScript)
				{
					if (arg1 == "all")
					{
						mob.RemoveFromWorld();

						DbMob mobs = GameServer.Database.FindObjectByKey<DbMob>(mob.InternalID);
						if (mobs != null)
						{
							mob.LoadFromDatabase(mobs);
							mob.AddToWorld();
							count++;
						}
					}

					if (arg1 == "realm")
					{
						ERealm realm = ERealm.None;
						if (arg2 == "None") realm = ERealm.None;
						if (arg2 == "Albion") realm = ERealm.Albion;
						if (arg2 == "Midgard") realm = ERealm.Midgard;
						if (arg2 == "Hibernia") realm = ERealm.Hibernia;

						if (mob.Realm == realm)
						{
							mob.RemoveFromWorld();

							DbMob mobs = GameServer.Database.FindObjectByKey<DbMob>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}

					if (arg1 == "name")
					{
						if (mob.Name == arg2)
						{
							mob.RemoveFromWorld();

							DbMob mobs = GameServer.Database.FindObjectByKey<DbMob>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}

					if (arg1 == "model")
					{
						if (mob.Model == Convert.ToUInt16(arg2))
						{
							mob.RemoveFromWorld();

							DbWorldObject mobs = GameServer.Database.FindObjectByKey<DbWorldObject>(mob.InternalID);
							if (mobs != null)
							{
								mob.LoadFromDatabase(mobs);
								mob.AddToWorld();
								count++;
							}
						}
					}
				}
			}

			ChatUtil.SendSystemMessage(player, count + " mobs reloaded!");
		}

		private void ReloadStaticItem(ushort region, string arg1, string arg2)
		{
			if (region == 0)
			{
				log.Info("Region reload not supported from console.");
				return;
			}

			foreach (GameStaticItem staticItem in WorldMgr.GetStaticItemFromRegion(region))
			{
				if (!staticItem.LoadedFromScript)
				{
					if (arg1 == "all")
					{
						staticItem.RemoveFromWorld();

						DbWorldObject obj = GameServer.Database.FindObjectByKey<DbWorldObject>(staticItem.InternalID);
						if (obj != null)
						{
							staticItem.LoadFromDatabase(obj);
							staticItem.AddToWorld();
						}
					}

					if (arg1 == "realm")
					{
						ERealm realm = ERealm.None;
						if (arg2 == "None") realm = ERealm.None;
						if (arg2 == "Albion") realm = ERealm.Albion;
						if (arg2 == "Midgard") realm = ERealm.Midgard;
						if (arg2 == "Hibernia") realm = ERealm.Hibernia;

						if (staticItem.Realm == realm)
						{
							staticItem.RemoveFromWorld();

							DbWorldObject obj = GameServer.Database.FindObjectByKey<DbWorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}

					if (arg1 == "name")
					{
						if (staticItem.Name == arg2)
						{
							staticItem.RemoveFromWorld();

							DbWorldObject obj = GameServer.Database.FindObjectByKey<DbWorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}

					if (arg1 == "model")
					{
						if (staticItem.Model == Convert.ToUInt16(arg2))
						{
							staticItem.RemoveFromWorld();

							DbWorldObject obj = GameServer.Database.FindObjectByKey<DbWorldObject>(staticItem.InternalID);
							if (obj != null)
							{
								staticItem.LoadFromDatabase(obj);
								staticItem.AddToWorld();
							}
						}
					}
				}
			}
		}
	}
}
