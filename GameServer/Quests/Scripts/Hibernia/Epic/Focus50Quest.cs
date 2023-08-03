using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Hibernia
{
	public class Focus50Quest : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Unnatural Powers";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;
		private int _GreenMawAddKilled = 0;

		private static GameNpc Ainrebh = null; // Start NPC
		private static EpicGreenMaw GreenMaw = null; // Mob to kill

		private static DbItemTemplates GreenMaw_key = null; //ball of flame
		private static DbItemTemplates RangerEpicBoots = null; //Mist Shrouded Boots 
		private static DbItemTemplates RangerEpicHelm = null; //Mist Shrouded Coif 
		private static DbItemTemplates RangerEpicGloves = null; //Mist Shrouded Gloves 
		private static DbItemTemplates RangerEpicVest = null; //Mist Shrouded Hauberk 
		private static DbItemTemplates RangerEpicLegs = null; //Mist Shrouded Legs 
		private static DbItemTemplates RangerEpicArms = null; //Mist Shrouded Sleeves 
		private static DbItemTemplates HeroEpicBoots = null; //Shadow Shrouded Boots 
		private static DbItemTemplates HeroEpicHelm = null; //Shadow Shrouded Coif 
		private static DbItemTemplates HeroEpicGloves = null; //Shadow Shrouded Gloves 
		private static DbItemTemplates HeroEpicVest = null; //Shadow Shrouded Hauberk 
		private static DbItemTemplates HeroEpicLegs = null; //Shadow Shrouded Legs 
		private static DbItemTemplates HeroEpicArms = null; //Shadow Shrouded Sleeves 
		private static DbItemTemplates EldritchEpicBoots = null; //Valhalla Touched Boots 
		private static DbItemTemplates EldritchEpicHelm = null; //Valhalla Touched Coif 
		private static DbItemTemplates EldritchEpicGloves = null; //Valhalla Touched Gloves 
		private static DbItemTemplates EldritchEpicVest = null; //Valhalla Touched Hauberk 
		private static DbItemTemplates EldritchEpicLegs = null; //Valhalla Touched Legs 
		private static DbItemTemplates EldritchEpicArms = null; //Valhalla Touched Sleeves 
		private static DbItemTemplates WardenEpicBoots = null; //Subterranean Boots 
		private static DbItemTemplates WardenEpicHelm = null; //Subterranean Coif 
		private static DbItemTemplates WardenEpicGloves = null; //Subterranean Gloves 
		private static DbItemTemplates WardenEpicVest = null; //Subterranean Hauberk 
		private static DbItemTemplates WardenEpicLegs = null; //Subterranean Legs 
		private static DbItemTemplates WardenEpicArms = null; //Subterranean Sleeves    
        private static DbItemTemplates MaulerHibEpicBoots = null;
        private static DbItemTemplates MaulerHibEpicHelm = null;
        private static DbItemTemplates MaulerHibEpicGloves = null;
        private static DbItemTemplates MaulerHibEpicVest = null;
        private static DbItemTemplates MaulerHibEpicLegs = null;
        private static DbItemTemplates MaulerHibEpicArms = null;      

		// Constructors
		public Focus50Quest() : base()
		{
		}

		public Focus50Quest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public Focus50Quest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public Focus50Quest(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			

			#region NPC Declarations

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Ainrebh", ERealm.Hibernia);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 421281 && npc.Y == 516273)
					{
						Ainrebh = npc;
						break;
					}

			if (Ainrebh == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Ainrebh , creating it ...");
				Ainrebh = new GameNpc();
				Ainrebh.Model = 384;
				Ainrebh.Name = "Ainrebh";
				Ainrebh.GuildName = "Enchanter";
				Ainrebh.Realm = ERealm.Hibernia;
				Ainrebh.CurrentRegionID = 200;
				Ainrebh.Size = 48;
				Ainrebh.Level = 40;
				Ainrebh.X = 421281;
				Ainrebh.Y = 516273;
				Ainrebh.Z = 1877;
				Ainrebh.Heading = 3254;
				Ainrebh.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Ainrebh.SaveIntoDatabase();
				}
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Green Maw", ERealm.None);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 200 && npc.X == 488306 && npc.Y == 521440)
					{
						GreenMaw = npc as EpicGreenMaw;
						break;
					}

			if (GreenMaw == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find GreenMaw , creating it ...");
				GreenMaw = new EpicGreenMaw();
				GreenMaw.Model = 146;
				GreenMaw.Name = "Green Maw";
				GreenMaw.GuildName = "";
				GreenMaw.Realm = ERealm.None;
				GreenMaw.CurrentRegionID = 200;
				GreenMaw.Size = 50;
				GreenMaw.Level = 65;
				GreenMaw.X = 488306;
				GreenMaw.Y = 521440;
				GreenMaw.Z = 6328;
				GreenMaw.Heading = 1162;
				GreenMaw.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					GreenMaw.SaveIntoDatabase();
				}
			}
			// end npc

			#endregion

			#region Item Declarations

			GreenMaw_key = GameServer.Database.FindObjectByKey<DbItemTemplates>("GreenMaw_key");
			if (GreenMaw_key == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find GreenMaw's Key , creating it ...");
				GreenMaw_key = new DbItemTemplates();
				GreenMaw_key.Id_nb = "GreenMaw_key";
				GreenMaw_key.Name = "GreenMaw's Key";
				GreenMaw_key.Level = 8;
				GreenMaw_key.Item_Type = 29;
				GreenMaw_key.Model = 583;
				GreenMaw_key.IsDropable = false;
				GreenMaw_key.IsPickable = false;
				GreenMaw_key.DPS_AF = 0;
				GreenMaw_key.SPD_ABS = 0;
				GreenMaw_key.Object_Type = 41;
				GreenMaw_key.Hand = 0;
				GreenMaw_key.Type_Damage = 0;
				GreenMaw_key.Quality = 100;
				GreenMaw_key.Weight = 12;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(GreenMaw_key);
				}

			}
// end item			
			RangerEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicBoots");
			if (RangerEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rangers Epic Boots , creating it ...");
				RangerEpicBoots = new DbItemTemplates();
				RangerEpicBoots.Id_nb = "RangerEpicBoots";
				RangerEpicBoots.Name = "Mist Shrouded Boots";
				RangerEpicBoots.Level = 50;
				RangerEpicBoots.Item_Type = 23;
				RangerEpicBoots.Model = 819;
				RangerEpicBoots.IsDropable = true;
				RangerEpicBoots.IsPickable = true;
				RangerEpicBoots.DPS_AF = 100;
				RangerEpicBoots.SPD_ABS = 19;
				RangerEpicBoots.Object_Type = 37;
				RangerEpicBoots.Quality = 100;
				RangerEpicBoots.Weight = 22;
				RangerEpicBoots.Bonus = 35;
				RangerEpicBoots.MaxCondition = 50000;
				RangerEpicBoots.MaxDurability = 50000;
				RangerEpicBoots.Condition = 50000;
				RangerEpicBoots.Durability = 50000;

				RangerEpicBoots.Bonus1 = 13;
				RangerEpicBoots.Bonus1Type = (int) EStat.DEX;

				RangerEpicBoots.Bonus2 = 12;
				RangerEpicBoots.Bonus2Type = (int) EStat.QUI;

				RangerEpicBoots.Bonus3 = 8;
				RangerEpicBoots.Bonus3Type = (int) EResist.Thrust;

				RangerEpicBoots.Bonus4 = 30;
				RangerEpicBoots.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicBoots);
				}

			}
//end item
			//Mist Shrouded Coif 
			RangerEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicHelm");
			if (RangerEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rangers Epic Helm , creating it ...");
				RangerEpicHelm = new DbItemTemplates();
				RangerEpicHelm.Id_nb = "RangerEpicHelm";
				RangerEpicHelm.Name = "Mist Shrouded Helm";
				RangerEpicHelm.Level = 50;
				RangerEpicHelm.Item_Type = 21;
				RangerEpicHelm.Model = 1292; //NEED TO WORK ON..
				RangerEpicHelm.IsDropable = true;
				RangerEpicHelm.IsPickable = true;
				RangerEpicHelm.DPS_AF = 100;
				RangerEpicHelm.SPD_ABS = 19;
				RangerEpicHelm.Object_Type = 37;
				RangerEpicHelm.Quality = 100;
				RangerEpicHelm.Weight = 22;
				RangerEpicHelm.Bonus = 35;
				RangerEpicHelm.MaxCondition = 50000;
				RangerEpicHelm.MaxDurability = 50000;
				RangerEpicHelm.Condition = 50000;
				RangerEpicHelm.Durability = 50000;

				RangerEpicHelm.Bonus1 = 19;
				RangerEpicHelm.Bonus1Type = (int) EStat.DEX;

				RangerEpicHelm.Bonus2 = 10;
				RangerEpicHelm.Bonus2Type = (int) EResist.Spirit;

				RangerEpicHelm.Bonus3 = 27;
				RangerEpicHelm.Bonus3Type = (int) EProperty.MaxHealth;

				RangerEpicHelm.Bonus4 = 10;
				RangerEpicHelm.Bonus4Type = (int) EResist.Energy;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicHelm);
				}

			}
//end item
			//Mist Shrouded Gloves 
			RangerEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicGloves");
			if (RangerEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rangers Epic Gloves , creating it ...");
				RangerEpicGloves = new DbItemTemplates();
				RangerEpicGloves.Id_nb = "RangerEpicGloves";
				RangerEpicGloves.Name = "Mist Shrouded Gloves ";
				RangerEpicGloves.Level = 50;
				RangerEpicGloves.Item_Type = 22;
				RangerEpicGloves.Model = 818;
				RangerEpicGloves.IsDropable = true;
				RangerEpicGloves.IsPickable = true;
				RangerEpicGloves.DPS_AF = 100;
				RangerEpicGloves.SPD_ABS = 19;
				RangerEpicGloves.Object_Type = 37;
				RangerEpicGloves.Quality = 100;
				RangerEpicGloves.Weight = 22;
				RangerEpicGloves.Bonus = 35;
				RangerEpicGloves.MaxCondition = 50000;
				RangerEpicGloves.MaxDurability = 50000;
				RangerEpicGloves.Condition = 50000;
				RangerEpicGloves.Durability = 50000;

				RangerEpicGloves.Bonus1 = 3;
				RangerEpicGloves.Bonus1Type = (int) EProperty.Skill_RecurvedBow;

				RangerEpicGloves.Bonus2 = 15;
				RangerEpicGloves.Bonus2Type = (int) EStat.DEX;

				RangerEpicGloves.Bonus3 = 15;
				RangerEpicGloves.Bonus3Type = (int) EStat.QUI;

				RangerEpicGloves.Bonus4 = 10;
				RangerEpicGloves.Bonus4Type = (int) EResist.Crush;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicGloves);
				}

			}
			//Mist Shrouded Hauberk 
			RangerEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicVest");
			if (RangerEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rangers Epic Vest , creating it ...");
				RangerEpicVest = new DbItemTemplates();
				RangerEpicVest.Id_nb = "RangerEpicVest";
				RangerEpicVest.Name = "Mist Shrouded Hauberk";
				RangerEpicVest.Level = 50;
				RangerEpicVest.Item_Type = 25;
				RangerEpicVest.Model = 815;
				RangerEpicVest.IsDropable = true;
				RangerEpicVest.IsPickable = true;
				RangerEpicVest.DPS_AF = 100;
				RangerEpicVest.SPD_ABS = 19;
				RangerEpicVest.Object_Type = 37;
				RangerEpicVest.Quality = 100;
				RangerEpicVest.Weight = 22;
				RangerEpicVest.Bonus = 35;
				RangerEpicVest.MaxCondition = 50000;
				RangerEpicVest.MaxDurability = 50000;
				RangerEpicVest.Condition = 50000;
				RangerEpicVest.Durability = 50000;

				RangerEpicVest.Bonus1 = 7;
				RangerEpicVest.Bonus1Type = (int) EStat.STR;

				RangerEpicVest.Bonus2 = 7;
				RangerEpicVest.Bonus2Type = (int) EStat.DEX;

				RangerEpicVest.Bonus3 = 7;
				RangerEpicVest.Bonus3Type = (int) EStat.QUI;

				RangerEpicVest.Bonus4 = 48;
				RangerEpicVest.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicVest);
				}

			}
			//Mist Shrouded Legs 
			RangerEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicLegs");
			if (RangerEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Rangers Epic Legs , creating it ...");
				RangerEpicLegs = new DbItemTemplates();
				RangerEpicLegs.Id_nb = "RangerEpicLegs";
				RangerEpicLegs.Name = "Mist Shrouded Leggings";
				RangerEpicLegs.Level = 50;
				RangerEpicLegs.Item_Type = 27;
				RangerEpicLegs.Model = 816;
				RangerEpicLegs.IsDropable = true;
				RangerEpicLegs.IsPickable = true;
				RangerEpicLegs.DPS_AF = 100;
				RangerEpicLegs.SPD_ABS = 19;
				RangerEpicLegs.Object_Type = 37;
				RangerEpicLegs.Quality = 100;
				RangerEpicLegs.Weight = 22;
				RangerEpicLegs.Bonus = 35;
				RangerEpicLegs.MaxCondition = 50000;
				RangerEpicLegs.MaxDurability = 50000;
				RangerEpicLegs.Condition = 50000;
				RangerEpicLegs.Durability = 50000;

				RangerEpicLegs.Bonus1 = 12;
				RangerEpicLegs.Bonus1Type = (int) EStat.STR;

				RangerEpicLegs.Bonus2 = 12;
				RangerEpicLegs.Bonus2Type = (int) EStat.CON;

				RangerEpicLegs.Bonus3 = 12;
				RangerEpicLegs.Bonus3Type = (int) EResist.Body;

				RangerEpicLegs.Bonus4 = 39;
				RangerEpicLegs.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicLegs);
				}
				;

			}
			//Mist Shrouded Sleeves 
			RangerEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("RangerEpicArms");
			if (RangerEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Ranger Epic Arms , creating it ...");
				RangerEpicArms = new DbItemTemplates();
				RangerEpicArms.Id_nb = "RangerEpicArms";
				RangerEpicArms.Name = "Mist Shrouded Sleeves";
				RangerEpicArms.Level = 50;
				RangerEpicArms.Item_Type = 28;
				RangerEpicArms.Model = 817;
				RangerEpicArms.IsDropable = true;
				RangerEpicArms.IsPickable = true;
				RangerEpicArms.DPS_AF = 100;
				RangerEpicArms.SPD_ABS = 19;
				RangerEpicArms.Object_Type = 37;
				RangerEpicArms.Quality = 100;
				RangerEpicArms.Weight = 22;
				RangerEpicArms.Bonus = 35;
				RangerEpicArms.MaxCondition = 50000;
				RangerEpicArms.MaxDurability = 50000;
				RangerEpicArms.Condition = 50000;
				RangerEpicArms.Durability = 50000;

				RangerEpicArms.Bonus1 = 12;
				RangerEpicArms.Bonus1Type = (int) EStat.STR;

				RangerEpicArms.Bonus2 = 12;
				RangerEpicArms.Bonus2Type = (int) EStat.DEX;

				RangerEpicArms.Bonus3 = 10;
				RangerEpicArms.Bonus3Type = (int) EResist.Spirit;

				RangerEpicArms.Bonus4 = 30;
				RangerEpicArms.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RangerEpicArms);
				}

			}
//Hero Epic Sleeves End
			HeroEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicBoots");
			if (HeroEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Heros Epic Boots , creating it ...");
				HeroEpicBoots = new DbItemTemplates();
				HeroEpicBoots.Id_nb = "HeroEpicBoots";
				HeroEpicBoots.Name = "Misted Boots";
				HeroEpicBoots.Level = 50;
				HeroEpicBoots.Item_Type = 23;
				HeroEpicBoots.Model = 712;
				HeroEpicBoots.IsDropable = true;
				HeroEpicBoots.IsPickable = true;
				HeroEpicBoots.DPS_AF = 100;
				HeroEpicBoots.SPD_ABS = 27;
				HeroEpicBoots.Object_Type = 38;
				HeroEpicBoots.Quality = 100;
				HeroEpicBoots.Weight = 22;
				HeroEpicBoots.Bonus = 35;
				HeroEpicBoots.MaxCondition = 50000;
				HeroEpicBoots.MaxDurability = 50000;
				HeroEpicBoots.Condition = 50000;
				HeroEpicBoots.Durability = 50000;

				HeroEpicBoots.Bonus1 = 12;
				HeroEpicBoots.Bonus1Type = (int) EStat.CON;

				HeroEpicBoots.Bonus2 = 12;
				HeroEpicBoots.Bonus2Type = (int) EStat.QUI;

				HeroEpicBoots.Bonus3 = 8;
				HeroEpicBoots.Bonus3Type = (int) EResist.Spirit;

				HeroEpicBoots.Bonus4 = 33;
				HeroEpicBoots.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicBoots);
				}

			}
//end item
			//Misted Coif 
			HeroEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicHelm");
			if (HeroEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Heros Epic Helm , creating it ...");
				HeroEpicHelm = new DbItemTemplates();
				HeroEpicHelm.Id_nb = "HeroEpicHelm";
				HeroEpicHelm.Name = "Misted Coif";
				HeroEpicHelm.Level = 50;
				HeroEpicHelm.Item_Type = 21;
				HeroEpicHelm.Model = 1292; //NEED TO WORK ON..
				HeroEpicHelm.IsDropable = true;
				HeroEpicHelm.IsPickable = true;
				HeroEpicHelm.DPS_AF = 100;
				HeroEpicHelm.SPD_ABS = 27;
				HeroEpicHelm.Object_Type = 38;
				HeroEpicHelm.Quality = 100;
				HeroEpicHelm.Weight = 22;
				HeroEpicHelm.Bonus = 35;
				HeroEpicHelm.MaxCondition = 50000;
				HeroEpicHelm.MaxDurability = 50000;
				HeroEpicHelm.Condition = 50000;
				HeroEpicHelm.Durability = 50000;

				HeroEpicHelm.Bonus1 = 15;
				HeroEpicHelm.Bonus1Type = (int) EStat.STR;

				HeroEpicHelm.Bonus2 = 8;
				HeroEpicHelm.Bonus2Type = (int) EResist.Spirit;

				HeroEpicHelm.Bonus3 = 48;
				HeroEpicHelm.Bonus3Type = (int) EProperty.MaxHealth;

				HeroEpicHelm.Bonus4 = 8;
				HeroEpicHelm.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicHelm);
				}

			}
//end item
			//Misted Gloves 
			HeroEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicGloves");
			if (HeroEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Heros Epic Gloves , creating it ...");
				HeroEpicGloves = new DbItemTemplates();
				HeroEpicGloves.Id_nb = "HeroEpicGloves";
				HeroEpicGloves.Name = "Misted Gloves ";
				HeroEpicGloves.Level = 50;
				HeroEpicGloves.Item_Type = 22;
				HeroEpicGloves.Model = 711;
				HeroEpicGloves.IsDropable = true;
				HeroEpicGloves.IsPickable = true;
				HeroEpicGloves.DPS_AF = 100;
				HeroEpicGloves.SPD_ABS = 27;
				HeroEpicGloves.Object_Type = 38;
				HeroEpicGloves.Quality = 100;
				HeroEpicGloves.Weight = 22;
				HeroEpicGloves.Bonus = 35;
				HeroEpicGloves.MaxCondition = 50000;
				HeroEpicGloves.MaxDurability = 50000;
				HeroEpicGloves.Condition = 50000;
				HeroEpicGloves.Durability = 50000;

				HeroEpicGloves.Bonus1 = 2;
				HeroEpicGloves.Bonus1Type = (int) EProperty.Skill_Shields;

				HeroEpicGloves.Bonus2 = 2;
				HeroEpicGloves.Bonus2Type = (int) EProperty.Skill_Parry;

				HeroEpicGloves.Bonus3 = 16;
				HeroEpicGloves.Bonus3Type = (int) EStat.DEX;

				HeroEpicGloves.Bonus4 = 18;
				HeroEpicGloves.Bonus4Type = (int) EStat.QUI;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicGloves);
				}

			}
			//Misted Hauberk 
			HeroEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicVest");
			if (HeroEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Heros Epic Vest , creating it ...");
				HeroEpicVest = new DbItemTemplates();
				HeroEpicVest.Id_nb = "HeroEpicVest";
				HeroEpicVest.Name = "Misted Hauberk";
				HeroEpicVest.Level = 50;
				HeroEpicVest.Item_Type = 25;
				HeroEpicVest.Model = 708;
				HeroEpicVest.IsDropable = true;
				HeroEpicVest.IsPickable = true;
				HeroEpicVest.DPS_AF = 100;
				HeroEpicVest.SPD_ABS = 27;
				HeroEpicVest.Object_Type = 38;
				HeroEpicVest.Quality = 100;
				HeroEpicVest.Weight = 22;
				HeroEpicVest.Bonus = 35;
				HeroEpicVest.MaxCondition = 50000;
				HeroEpicVest.MaxDurability = 50000;
				HeroEpicVest.Condition = 50000;
				HeroEpicVest.Durability = 50000;

				HeroEpicVest.Bonus1 = 15;
				HeroEpicVest.Bonus1Type = (int) EStat.STR;

				HeroEpicVest.Bonus2 = 16;
				HeroEpicVest.Bonus2Type = (int) EStat.CON;

				HeroEpicVest.Bonus3 = 15;
				HeroEpicVest.Bonus3Type = (int) EStat.DEX;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicVest);
				}

			}
			//Misted Legs 
			HeroEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicLegs");
			if (HeroEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Heros Epic Legs , creating it ...");
				HeroEpicLegs = new DbItemTemplates();
				HeroEpicLegs.Id_nb = "HeroEpicLegs";
				HeroEpicLegs.Name = "Misted Leggings";
				HeroEpicLegs.Level = 50;
				HeroEpicLegs.Item_Type = 27;
				HeroEpicLegs.Model = 709;
				HeroEpicLegs.IsDropable = true;
				HeroEpicLegs.IsPickable = true;
				HeroEpicLegs.DPS_AF = 100;
				HeroEpicLegs.SPD_ABS = 27;
				HeroEpicLegs.Object_Type = 38;
				HeroEpicLegs.Quality = 100;
				HeroEpicLegs.Weight = 22;
				HeroEpicLegs.Bonus = 35;
				HeroEpicLegs.MaxCondition = 50000;
				HeroEpicLegs.MaxDurability = 50000;
				HeroEpicLegs.Condition = 50000;
				HeroEpicLegs.Durability = 50000;

				HeroEpicLegs.Bonus1 = 10;
				HeroEpicLegs.Bonus1Type = (int) EStat.STR;

				HeroEpicLegs.Bonus2 = 21;
				HeroEpicLegs.Bonus2Type = (int) EStat.CON;

				HeroEpicLegs.Bonus3 = 10;
				HeroEpicLegs.Bonus3Type = (int) EResist.Thrust;

				HeroEpicLegs.Bonus4 = 10;
				HeroEpicLegs.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicLegs);
				}

			}
			//Misted Sleeves 
			HeroEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("HeroEpicArms");
			if (HeroEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Hero Epic Arms , creating it ...");
				HeroEpicArms = new DbItemTemplates();
				HeroEpicArms.Id_nb = "HeroEpicArms";
				HeroEpicArms.Name = "Misted Sleeves";
				HeroEpicArms.Level = 50;
				HeroEpicArms.Item_Type = 28;
				HeroEpicArms.Model = 710;
				HeroEpicArms.IsDropable = true;
				HeroEpicArms.IsPickable = true;
				HeroEpicArms.DPS_AF = 100;
				HeroEpicArms.SPD_ABS = 27;
				HeroEpicArms.Object_Type = 38;
				HeroEpicArms.Quality = 100;
				HeroEpicArms.Weight = 22;
				HeroEpicArms.Bonus = 35;
				HeroEpicArms.MaxCondition = 50000;
				HeroEpicArms.MaxDurability = 50000;
				HeroEpicArms.Condition = 50000;
				HeroEpicArms.Durability = 50000;

				HeroEpicArms.Bonus1 = 24;
				HeroEpicArms.Bonus1Type = (int) EStat.STR;

				HeroEpicArms.Bonus2 = 10;
				HeroEpicArms.Bonus2Type = (int) EStat.DEX;

				HeroEpicArms.Bonus3 = 8;
				HeroEpicArms.Bonus3Type = (int) EResist.Cold;

				HeroEpicArms.Bonus4 = 8;
				HeroEpicArms.Bonus4Type = (int) EResist.Spirit;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(HeroEpicArms);
				}

			}
			WardenEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicBoots");
			if (WardenEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Boots , creating it ...");
				WardenEpicBoots = new DbItemTemplates();
				WardenEpicBoots.Id_nb = "WardenEpicBoots";
				WardenEpicBoots.Name = "Mystical Boots";
				WardenEpicBoots.Level = 50;
				WardenEpicBoots.Item_Type = 23;
				WardenEpicBoots.Model = 809;
				WardenEpicBoots.IsDropable = true;
				WardenEpicBoots.IsPickable = true;
				WardenEpicBoots.DPS_AF = 100;
				WardenEpicBoots.SPD_ABS = 27;
				WardenEpicBoots.Object_Type = 38;
				WardenEpicBoots.Quality = 100;
				WardenEpicBoots.Weight = 22;
				WardenEpicBoots.Bonus = 35;
				WardenEpicBoots.MaxCondition = 50000;
				WardenEpicBoots.MaxDurability = 50000;
				WardenEpicBoots.Condition = 50000;
				WardenEpicBoots.Durability = 50000;

				WardenEpicBoots.Bonus1 = 15;
				WardenEpicBoots.Bonus1Type = (int) EStat.DEX;

				WardenEpicBoots.Bonus2 = 16;
				WardenEpicBoots.Bonus2Type = (int) EStat.QUI;

				WardenEpicBoots.Bonus3 = 10;
				WardenEpicBoots.Bonus3Type = (int) EResist.Crush;

				WardenEpicBoots.Bonus4 = 10;
				WardenEpicBoots.Bonus4Type = (int) EResist.Matter;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicBoots);
				}

			}
//end item
			//Mystical Coif 
			WardenEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicHelm");
			if (WardenEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Helm , creating it ...");
				WardenEpicHelm = new DbItemTemplates();
				WardenEpicHelm.Id_nb = "WardenEpicHelm";
				WardenEpicHelm.Name = "Mystical Coif";
				WardenEpicHelm.Level = 50;
				WardenEpicHelm.Item_Type = 21;
				WardenEpicHelm.Model = 1292; //NEED TO WORK ON..
				WardenEpicHelm.IsDropable = true;
				WardenEpicHelm.IsPickable = true;
				WardenEpicHelm.DPS_AF = 100;
				WardenEpicHelm.SPD_ABS = 27;
				WardenEpicHelm.Object_Type = 38;
				WardenEpicHelm.Quality = 100;
				WardenEpicHelm.Weight = 22;
				WardenEpicHelm.Bonus = 35;
				WardenEpicHelm.MaxCondition = 50000;
				WardenEpicHelm.MaxDurability = 50000;
				WardenEpicHelm.Condition = 50000;
				WardenEpicHelm.Durability = 50000;

				WardenEpicHelm.Bonus1 = 15;
				WardenEpicHelm.Bonus1Type = (int) EStat.EMP;

				WardenEpicHelm.Bonus2 = 2;
				WardenEpicHelm.Bonus2Type = (int) EProperty.PowerRegenerationRate;

				WardenEpicHelm.Bonus3 = 30;
				WardenEpicHelm.Bonus3Type = (int) EProperty.MaxHealth;

				WardenEpicHelm.Bonus4 = 4;
				WardenEpicHelm.Bonus4Type = (int) EProperty.Skill_Regrowth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicHelm);
				}

			}
//end item
			//Mystical Gloves 
			WardenEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicGloves");
			if (WardenEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Gloves , creating it ...");
				WardenEpicGloves = new DbItemTemplates();
				WardenEpicGloves.Id_nb = "WardenEpicGloves";
				WardenEpicGloves.Name = "Mystical Gloves ";
				WardenEpicGloves.Level = 50;
				WardenEpicGloves.Item_Type = 22;
				WardenEpicGloves.Model = 808;
				WardenEpicGloves.IsDropable = true;
				WardenEpicGloves.IsPickable = true;
				WardenEpicGloves.DPS_AF = 100;
				WardenEpicGloves.SPD_ABS = 27;
				WardenEpicGloves.Object_Type = 38;
				WardenEpicGloves.Quality = 100;
				WardenEpicGloves.Weight = 22;
				WardenEpicGloves.Bonus = 35;
				WardenEpicGloves.MaxCondition = 50000;
				WardenEpicGloves.MaxDurability = 50000;
				WardenEpicGloves.Condition = 50000;
				WardenEpicGloves.Durability = 50000;

				WardenEpicGloves.Bonus1 = 4;
				WardenEpicGloves.Bonus1Type = (int) EProperty.Skill_Nurture;

				WardenEpicGloves.Bonus2 = 12;
				WardenEpicGloves.Bonus2Type = (int) EResist.Slash;

				WardenEpicGloves.Bonus3 = 4;
				WardenEpicGloves.Bonus3Type = (int) EProperty.PowerRegenerationRate;

				WardenEpicGloves.Bonus4 = 33;
				WardenEpicGloves.Bonus4Type = (int) EProperty.MaxHealth;


				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicGloves);
				}

			}
			//Mystical Hauberk 
			WardenEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicVest");
			if (WardenEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Vest , creating it ...");
				WardenEpicVest = new DbItemTemplates();
				WardenEpicVest.Id_nb = "WardenEpicVest";
				WardenEpicVest.Name = "Mystical Hauberk";
				WardenEpicVest.Level = 50;
				WardenEpicVest.Item_Type = 25;
				WardenEpicVest.Model = 805;
				WardenEpicVest.IsDropable = true;
				WardenEpicVest.IsPickable = true;
				WardenEpicVest.DPS_AF = 100;
				WardenEpicVest.SPD_ABS = 27;
				WardenEpicVest.Object_Type = 38;
				WardenEpicVest.Quality = 100;
				WardenEpicVest.Weight = 22;
				WardenEpicVest.Bonus = 35;
				WardenEpicVest.MaxCondition = 50000;
				WardenEpicVest.MaxDurability = 50000;
				WardenEpicVest.Condition = 50000;
				WardenEpicVest.Durability = 50000;

				WardenEpicVest.Bonus1 = 9;
				WardenEpicVest.Bonus1Type = (int) EStat.STR;

				WardenEpicVest.Bonus2 = 9;
				WardenEpicVest.Bonus2Type = (int) EStat.DEX;

				WardenEpicVest.Bonus3 = 9;
				WardenEpicVest.Bonus3Type = (int) EStat.EMP;

				WardenEpicVest.Bonus2 = 39;
				WardenEpicVest.Bonus2Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicVest);
				}

			}
			//Mystical Legs 
			WardenEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicLegs");
			if (WardenEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Legs , creating it ...");
				WardenEpicLegs = new DbItemTemplates();
				WardenEpicLegs.Id_nb = "WardenEpicLegs";
				WardenEpicLegs.Name = "Mystical Legs";
				WardenEpicLegs.Level = 50;
				WardenEpicLegs.Item_Type = 27;
				WardenEpicLegs.Model = 806;
				WardenEpicLegs.IsDropable = true;
				WardenEpicLegs.IsPickable = true;
				WardenEpicLegs.DPS_AF = 100;
				WardenEpicLegs.SPD_ABS = 27;
				WardenEpicLegs.Object_Type = 38;
				WardenEpicLegs.Quality = 100;
				WardenEpicLegs.Weight = 22;
				WardenEpicLegs.Bonus = 35;
				WardenEpicLegs.MaxCondition = 50000;
				WardenEpicLegs.MaxDurability = 50000;
				WardenEpicLegs.Condition = 50000;
				WardenEpicLegs.Durability = 50000;

				WardenEpicLegs.Bonus1 = 10;
				WardenEpicLegs.Bonus1Type = (int) EStat.STR;

				WardenEpicLegs.Bonus2 = 10;
				WardenEpicLegs.Bonus2Type = (int) EStat.CON;

				WardenEpicLegs.Bonus3 = 10;
				WardenEpicLegs.Bonus3Type = (int) EStat.DEX;

				WardenEpicLegs.Bonus4 = 30;
				WardenEpicLegs.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicLegs);
				}

			}
			//Mystical Sleeves 
			WardenEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("WardenEpicArms");
			if (WardenEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warden Epic Arms , creating it ...");
				WardenEpicArms = new DbItemTemplates();
				WardenEpicArms.Id_nb = "WardenEpicArms";
				WardenEpicArms.Name = "Mystical Sleeves";
				WardenEpicArms.Level = 50;
				WardenEpicArms.Item_Type = 28;
				WardenEpicArms.Model = 807;
				WardenEpicArms.IsDropable = true;
				WardenEpicArms.IsPickable = true;
				WardenEpicArms.DPS_AF = 100;
				WardenEpicArms.SPD_ABS = 27;
				WardenEpicArms.Object_Type = 38;
				WardenEpicArms.Quality = 100;
				WardenEpicArms.Weight = 22;
				WardenEpicArms.Bonus = 35;
				WardenEpicArms.MaxCondition = 50000;
				WardenEpicArms.MaxDurability = 50000;
				WardenEpicArms.Condition = 50000;
				WardenEpicArms.Durability = 50000;

				WardenEpicArms.Bonus1 = 12;
				WardenEpicArms.Bonus1Type = (int) EStat.STR;

				WardenEpicArms.Bonus2 = 8;
				WardenEpicArms.Bonus2Type = (int) EResist.Matter;

				WardenEpicArms.Bonus3 = 8;
				WardenEpicArms.Bonus3Type = (int) EResist.Spirit;

				WardenEpicArms.Bonus4 = 45;
				WardenEpicArms.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WardenEpicArms);
				}

			}
			EldritchEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicBoots");
			if (EldritchEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Boots , creating it ...");
				EldritchEpicBoots = new DbItemTemplates();
				EldritchEpicBoots.Id_nb = "EldritchEpicBoots";
				EldritchEpicBoots.Name = "Mistwoven Boots";
				EldritchEpicBoots.Level = 50;
				EldritchEpicBoots.Item_Type = 23;
				EldritchEpicBoots.Model = 382;
				EldritchEpicBoots.IsDropable = true;
				EldritchEpicBoots.IsPickable = true;
				EldritchEpicBoots.DPS_AF = 50;
				EldritchEpicBoots.SPD_ABS = 0;
				EldritchEpicBoots.Object_Type = 32;
				EldritchEpicBoots.Quality = 100;
				EldritchEpicBoots.Weight = 22;
				EldritchEpicBoots.Bonus = 35;
				EldritchEpicBoots.MaxCondition = 50000;
				EldritchEpicBoots.MaxDurability = 50000;
				EldritchEpicBoots.Condition = 50000;
				EldritchEpicBoots.Durability = 50000;

				EldritchEpicBoots.Bonus1 = 9;
				EldritchEpicBoots.Bonus1Type = (int) EStat.CON;

				EldritchEpicBoots.Bonus2 = 9;
				EldritchEpicBoots.Bonus2Type = (int) EStat.DEX;

				EldritchEpicBoots.Bonus3 = 6;
				EldritchEpicBoots.Bonus3Type = (int) EProperty.PowerRegenerationRate;

				EldritchEpicBoots.Bonus4 = 21;
				EldritchEpicBoots.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicBoots);
				}

			}
//end item
			//Mist Woven Coif 
			EldritchEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicHelm");
			if (EldritchEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Helm , creating it ...");
				EldritchEpicHelm = new DbItemTemplates();
				EldritchEpicHelm.Id_nb = "EldritchEpicHelm";
				EldritchEpicHelm.Name = "Mistwoven Cap";
				EldritchEpicHelm.Level = 50;
				EldritchEpicHelm.Item_Type = 21;
				EldritchEpicHelm.Model = 1298; //NEED TO WORK ON..
				EldritchEpicHelm.IsDropable = true;
				EldritchEpicHelm.IsPickable = true;
				EldritchEpicHelm.DPS_AF = 50;
				EldritchEpicHelm.SPD_ABS = 0;
				EldritchEpicHelm.Object_Type = 32;
				EldritchEpicHelm.Quality = 100;
				EldritchEpicHelm.Weight = 22;
				EldritchEpicHelm.Bonus = 35;
				EldritchEpicHelm.MaxCondition = 50000;
				EldritchEpicHelm.MaxDurability = 50000;
				EldritchEpicHelm.Condition = 50000;
				EldritchEpicHelm.Durability = 50000;

				EldritchEpicHelm.Bonus1 = 10;
				EldritchEpicHelm.Bonus1Type = (int) EResist.Heat;

				EldritchEpicHelm.Bonus2 = 10;
				EldritchEpicHelm.Bonus2Type = (int) EResist.Spirit;

				EldritchEpicHelm.Bonus3 = 4;
				EldritchEpicHelm.Bonus3Type = (int) EProperty.Focus_Void;

				EldritchEpicHelm.Bonus4 = 19;
				EldritchEpicHelm.Bonus4Type = (int) EStat.INT;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicHelm);
				}

			}
//end item
			//Mist Woven Gloves 
			EldritchEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicGloves");
			if (EldritchEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Gloves , creating it ...");
				EldritchEpicGloves = new DbItemTemplates();
				EldritchEpicGloves.Id_nb = "EldritchEpicGloves";
				EldritchEpicGloves.Name = "Mistwoven Gloves ";
				EldritchEpicGloves.Level = 50;
				EldritchEpicGloves.Item_Type = 22;
				EldritchEpicGloves.Model = 381;
				EldritchEpicGloves.IsDropable = true;
				EldritchEpicGloves.IsPickable = true;
				EldritchEpicGloves.DPS_AF = 50;
				EldritchEpicGloves.SPD_ABS = 0;
				EldritchEpicGloves.Object_Type = 32;
				EldritchEpicGloves.Quality = 100;
				EldritchEpicGloves.Weight = 22;
				EldritchEpicGloves.Bonus = 35;
				EldritchEpicGloves.MaxCondition = 50000;
				EldritchEpicGloves.MaxDurability = 50000;
				EldritchEpicGloves.Condition = 50000;
				EldritchEpicGloves.Durability = 50000;

				EldritchEpicGloves.Bonus1 = 4;
				EldritchEpicGloves.Bonus1Type = (int) EProperty.Focus_Light;

				EldritchEpicGloves.Bonus2 = 9;
				EldritchEpicGloves.Bonus2Type = (int) EStat.DEX;

				EldritchEpicGloves.Bonus3 = 4;
				EldritchEpicGloves.Bonus3Type = (int) EProperty.PowerRegenerationRate;

				EldritchEpicGloves.Bonus4 = 24;
				EldritchEpicGloves.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicGloves);
				}

			}
			//Mist Woven Hauberk 
			EldritchEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicVest");
			if (EldritchEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Vest , creating it ...");
				EldritchEpicVest = new DbItemTemplates();
				EldritchEpicVest.Id_nb = "EldritchEpicVest";
				EldritchEpicVest.Name = "Mistwoven Vest";
				EldritchEpicVest.Level = 50;
				EldritchEpicVest.Item_Type = 25;
				EldritchEpicVest.Model = 744;
				EldritchEpicVest.IsDropable = true;
				EldritchEpicVest.IsPickable = true;
				EldritchEpicVest.DPS_AF = 50;
				EldritchEpicVest.SPD_ABS = 0;
				EldritchEpicVest.Object_Type = 32;
				EldritchEpicVest.Quality = 100;
				EldritchEpicVest.Weight = 22;
				EldritchEpicVest.Bonus = 35;
				EldritchEpicVest.MaxCondition = 50000;
				EldritchEpicVest.MaxDurability = 50000;
				EldritchEpicVest.Condition = 50000;
				EldritchEpicVest.Durability = 50000;

				EldritchEpicVest.Bonus1 = 15;
				EldritchEpicVest.Bonus1Type = (int) EStat.DEX;

				EldritchEpicVest.Bonus2 = 15;
				EldritchEpicVest.Bonus2Type = (int) EStat.INT;

				EldritchEpicVest.Bonus3 = 33;
				EldritchEpicVest.Bonus3 = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicVest);
				}

			}
			//Mist Woven Legs 
			EldritchEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicLegs");
			if (EldritchEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Legs , creating it ...");
				EldritchEpicLegs = new DbItemTemplates();
				EldritchEpicLegs.Id_nb = "EldritchEpicLegs";
				EldritchEpicLegs.Name = "Mistwoven Pants";
				EldritchEpicLegs.Level = 50;
				EldritchEpicLegs.Item_Type = 27;
				EldritchEpicLegs.Model = 379;
				EldritchEpicLegs.IsDropable = true;
				EldritchEpicLegs.IsPickable = true;
				EldritchEpicLegs.DPS_AF = 50;
				EldritchEpicLegs.SPD_ABS = 0;
				EldritchEpicLegs.Object_Type = 32;
				EldritchEpicLegs.Quality = 100;
				EldritchEpicLegs.Weight = 22;
				EldritchEpicLegs.Bonus = 35;
				EldritchEpicLegs.MaxCondition = 50000;
				EldritchEpicLegs.MaxDurability = 50000;
				EldritchEpicLegs.Condition = 50000;
				EldritchEpicLegs.Durability = 50000;

				EldritchEpicLegs.Bonus1 = 10;
				EldritchEpicLegs.Bonus1Type = (int) EResist.Cold;

				EldritchEpicLegs.Bonus2 = 10;
				EldritchEpicLegs.Bonus2Type = (int) EResist.Body;

				EldritchEpicLegs.Bonus3 = 15;
				EldritchEpicLegs.Bonus3Type = (int) EStat.DEX;

				EldritchEpicLegs.Bonus4 = 16;
				EldritchEpicLegs.Bonus4Type = (int) EStat.CON;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicLegs);
				}

			}
			//Mist Woven Sleeves 
			EldritchEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("EldritchEpicArms");
			if (EldritchEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Eldritch Epic Arms , creating it ...");
				EldritchEpicArms = new DbItemTemplates();
				EldritchEpicArms.Id_nb = "EldritchEpicArms";
				EldritchEpicArms.Name = "Mistwoven Sleeves";
				EldritchEpicArms.Level = 50;
				EldritchEpicArms.Item_Type = 28;
				EldritchEpicArms.Model = 380;
				EldritchEpicArms.IsDropable = true;
				EldritchEpicArms.IsPickable = true;
				EldritchEpicArms.DPS_AF = 50;
				EldritchEpicArms.SPD_ABS = 0;
				EldritchEpicArms.Object_Type = 32;
				EldritchEpicArms.Quality = 100;
				EldritchEpicArms.Weight = 22;
				EldritchEpicArms.Bonus = 35;
				EldritchEpicArms.MaxCondition = 50000;
				EldritchEpicArms.MaxDurability = 50000;
				EldritchEpicArms.Condition = 50000;
				EldritchEpicArms.Durability = 50000;

				EldritchEpicArms.Bonus1 = 4;
				EldritchEpicArms.Bonus1Type = (int) EProperty.Focus_Mana;

				EldritchEpicArms.Bonus2 = 10;
				EldritchEpicArms.Bonus2Type = (int) EStat.DEX;

				EldritchEpicArms.Bonus3 = 10;
				EldritchEpicArms.Bonus3Type = (int) EStat.INT;

				EldritchEpicArms.Bonus4 = 27;
				EldritchEpicArms.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(EldritchEpicArms);
				}

			}

//Hero Epic Sleeves End

            // Graveen: we assume items are existing in the DB
            // TODO: insert here creation of items if they do not exists
            MaulerHibEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicBoots");
            MaulerHibEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicHelm");
            MaulerHibEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicGloves");
            MaulerHibEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicVest");
            MaulerHibEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicLegs");
            MaulerHibEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerHibEpicArms");

//Item Descriptions End

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Ainrebh, GameObjectEvent.Interact, new CoreEventHandler(TalkToAinrebh));
			GameEventMgr.AddHandler(Ainrebh, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAinrebh));

			/* Now we bring to Ainrebh the possibility to give this quest to players */
			Ainrebh.AddQuestToGive(typeof (Focus50Quest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Ainrebh == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Ainrebh, GameObjectEvent.Interact, new CoreEventHandler(TalkToAinrebh));
			GameEventMgr.RemoveHandler(Ainrebh, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToAinrebh));

			/* Now we remove to Ainrebh the possibility to give this quest to players */
			Ainrebh.RemoveQuestToGive(typeof (Focus50Quest));
		}

		protected static void TalkToAinrebh(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if (Ainrebh.CanGiveQuest(typeof (Focus50Quest), player)  <= 0)
				return;

			// player is not allowed to start this quest until the quest rewards are available
			if (player.CharacterClass.ID == (byte)ECharacterClass.MaulerHib &&
				(MaulerHibEpicBoots == null || MaulerHibEpicBoots == null || MaulerHibEpicGloves == null ||
				MaulerHibEpicHelm == null || MaulerHibEpicLegs == null || MaulerHibEpicVest == null))
			{
				Ainrebh.SayTo(player, "This quest is not available to Maulers yet.");
				return;
			}

			//We also check if the player is already doing the quest
			Focus50Quest quest = player.IsDoingQuest(typeof (Focus50Quest)) as Focus50Quest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Ainrebh.SayTo(player, "Seek out Green Maw in Cursed Forest and kill it. Green Maw is guarded by grannies and spectral manslayers.");
							break;
						case 2:
							Ainrebh.SayTo(player, "Were you able to [fulfill] your given task?");
							break;
					}
				}
				else
				{
					Ainrebh.SayTo(player, "Hibernia needs your [services]!");
				}

			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				//Check player is already doing quest
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "services":
							player.Out.SendQuestSubscribeCommand(Ainrebh, QuestMgr.GetIDForQuestType(typeof(Focus50Quest)), "Will you help Ainrebh [Path of Focus Level 50 Epic]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "fulfill":
							if (quest.Step == 2)
							{
								RemoveItem(player, GreenMaw_key);
								if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
									    eInventorySlot.LastBackpack))
								{
									Ainrebh.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
									quest.FinishQuest();
								}
								else
									player.Out.SendMessage("You do not have enough free space in your inventory!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}
			}
			else if (e == GameObjectEvent.ReceiveItem)
			{
				var rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
					if (rArgs.Item.Id_nb == GreenMaw_key.Id_nb && quest.Step == 2)
					{
						if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
							    eInventorySlot.LastBackpack))
						{
							Ainrebh.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
							quest.FinishQuest();
						}
						else
							player.Out.SendMessage("You do not have enough free space in your inventory!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
					}
			}
		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (Focus50Quest)) != null)
				return true;

			if (player.CharacterClass.ID != (byte) ECharacterClass.Hero &&
				player.CharacterClass.ID != (byte) ECharacterClass.Ranger &&
                player.CharacterClass.ID != (byte) ECharacterClass.MaulerHib &&
                player.CharacterClass.ID != (byte) ECharacterClass.Warden &&
				player.CharacterClass.ID != (byte) ECharacterClass.Eldritch)
				return false;

			// This checks below are only performed is player isn't doing quest already

			//if (player.HasFinishedQuest(typeof(Academy_47)) == 0) return false;

			//if (!CheckPartAccessible(player,typeof(CityOfCamelot)))
			//	return false;

			if (player.Level < minimumLevel || player.Level > maximumLevel)
				return false;

			return true;
		}

		/* This is our callback hook that will be called when the player clicks
		 * on any button in the quest offer dialog. We check if he accepts or
		 * declines here...
		 */

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			Focus50Quest quest = player.IsDoingQuest(typeof (Focus50Quest)) as Focus50Quest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, no go out there and finish your work!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(Focus50Quest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Ainrebh.CanGiveQuest(typeof (Focus50Quest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (Focus50Quest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Ainrebh.GiveQuest(typeof (Focus50Quest), player, 1))
					return;
				player.Out.SendMessage("Kill Green Maw in Cursed Forest loc 37k, 38k!", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "Unnatural Powers (Level 50 Path of Focus Epic)"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Seek out Green Maw in Cursed Forest and kill it!";
					case 2:
						return "Return to Ainrebh and give her Green Maw's Key!";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (Focus50Quest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;

				if (gArgs.Target is GreenMawAdd3)//it must be last yellow adds 
                {
					_GreenMawAddKilled++;//count killed adds here
				}

				if (_GreenMawAddKilled >= 2)
				{
					m_questPlayer.Out.SendMessage("You collect Green Maw's Key", EChatType.CT_System, EChatLoc.CL_SystemWindow);
					GiveItem(player, GreenMaw_key);
					Step = 2;
				}
			}	
			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Ainrebh.Name && gArgs.Item.Id_nb == GreenMaw_key.Id_nb)
				{
					if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
						    eInventorySlot.LastBackpack))
					{
						Ainrebh.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
						FinishQuest();
					}
					else
						player.Out.SendMessage("You do not have enough free space in your inventory!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
				}
			}
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			RemoveItem(m_questPlayer, GreenMaw_key, false);
		}

		public override void FinishQuest()
		{
			RemoveItem(Ainrebh, m_questPlayer, GreenMaw_key);

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Hero)
			{
				GiveItem(m_questPlayer, HeroEpicArms);
				GiveItem(m_questPlayer, HeroEpicBoots);
				GiveItem(m_questPlayer, HeroEpicGloves);
				GiveItem(m_questPlayer, HeroEpicHelm);
				GiveItem(m_questPlayer, HeroEpicLegs);
				GiveItem(m_questPlayer, HeroEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Ranger)
			{
				GiveItem(m_questPlayer, RangerEpicArms);
				GiveItem(m_questPlayer, RangerEpicBoots);
				GiveItem(m_questPlayer, RangerEpicGloves);
				GiveItem(m_questPlayer, RangerEpicHelm);
				GiveItem(m_questPlayer, RangerEpicLegs);
				GiveItem(m_questPlayer, RangerEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Eldritch)
			{
				GiveItem(m_questPlayer, EldritchEpicArms);
				GiveItem(m_questPlayer, EldritchEpicBoots);
				GiveItem(m_questPlayer, EldritchEpicGloves);
				GiveItem(m_questPlayer, EldritchEpicHelm);
				GiveItem(m_questPlayer, EldritchEpicLegs);
				GiveItem(m_questPlayer, EldritchEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Warden)
			{
				GiveItem(m_questPlayer, WardenEpicArms);
				GiveItem(m_questPlayer, WardenEpicBoots);
				GiveItem(m_questPlayer, WardenEpicGloves);
				GiveItem(m_questPlayer, WardenEpicHelm);
				GiveItem(m_questPlayer, WardenEpicLegs);
				GiveItem(m_questPlayer, WardenEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.MaulerHib)
			{
				GiveItem(m_questPlayer, MaulerHibEpicBoots);
				GiveItem(m_questPlayer, MaulerHibEpicArms);
				GiveItem(m_questPlayer, MaulerHibEpicGloves);
				GiveItem(m_questPlayer, MaulerHibEpicHelm);
				GiveItem(m_questPlayer, MaulerHibEpicVest);
				GiveItem(m_questPlayer, MaulerHibEpicLegs);
			}

			m_questPlayer.GainExperience(EXpSource.Quest, 1937768448, true);
			//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
		}

		#region Allakhazam Epic Source

		/*
        *#25 talk to Ainrebh
        *#26 seek out Loken in Raumarik Loc 47k, 25k, 4k, and kill him purp and 2 blue adds 
        *#27 return to Ainrebh 
        *#28 give her the ball of flame
        *#29 talk with Ainrebh about Loken�s demise
        *#30 go to MorlinCaan in Jordheim 
        *#31 give her the sealed pouch
        *#32 you get your epic armor as a reward
        */

		/*
            *Mist Shrouded Boots 
            *Mist Shrouded Coif
            *Mist Shrouded Gloves
            *Mist Shrouded Hauberk
            *Mist Shrouded Legs
            *Mist Shrouded Sleeves
            *Shadow Shrouded Boots
            *Shadow Shrouded Coif
            *Shadow Shrouded Gloves
            *Shadow Shrouded Hauberk
            *Shadow Shrouded Legs
            *Shadow Shrouded Sleeves
        */

		#endregion
	}
}