using System;
using System.Reflection;
using DOL.AI.Brain;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Albion
{
	public class Academy50Quest : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Symbol of the Broken";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;
		protected private int _DemonicMinionsKilled = 0;
		protected private int _BechardKilled = 0;
		protected private int _SilchardeKilled = 0;

		private static GameNpc Ferowl = null; // Start NPC
		private static Morgana Morgana = null; // Mob
		private static Bechard Bechard = null; // Mob to kill
		private static Silcharde Silcharde = null; // Mob to kill

		//private static IArea morganaArea = null;

		private static DbItemTemplates sealed_pouch = null; //sealed pouch
		private static DbItemTemplates WizardEpicBoots = null; //Bernor's Numinous Boots 
		private static DbItemTemplates WizardEpicHelm = null; //Bernor's Numinous Coif 
		private static DbItemTemplates WizardEpicGloves = null; //Bernor's Numinous Gloves 
		private static DbItemTemplates WizardEpicVest = null; //Bernor's Numinous Hauberk 
		private static DbItemTemplates WizardEpicLegs = null; //Bernor's Numinous Legs 
		private static DbItemTemplates WizardEpicArms = null; //Bernor's Numinous Sleeves 
		private static DbItemTemplates MinstrelEpicBoots = null; //Shadow Shrouded Boots 
		private static DbItemTemplates MinstrelEpicHelm = null; //Shadow Shrouded Coif 
		private static DbItemTemplates MinstrelEpicGloves = null; //Shadow Shrouded Gloves 
		private static DbItemTemplates MinstrelEpicVest = null; //Shadow Shrouded Hauberk 
		private static DbItemTemplates MinstrelEpicLegs = null; //Shadow Shrouded Legs 
		private static DbItemTemplates MinstrelEpicArms = null; //Shadow Shrouded Sleeves 
		private static DbItemTemplates SorcerorEpicBoots = null; //Valhalla Touched Boots 
		private static DbItemTemplates SorcerorEpicHelm = null; //Valhalla Touched Coif 
		private static DbItemTemplates SorcerorEpicGloves = null; //Valhalla Touched Gloves 
		private static DbItemTemplates SorcerorEpicVest = null; //Valhalla Touched Hauberk 
		private static DbItemTemplates SorcerorEpicLegs = null; //Valhalla Touched Legs 
		private static DbItemTemplates SorcerorEpicArms = null; //Valhalla Touched Sleeves                 

		// Constructors
		public Academy50Quest() : base()
		{
		}

		public Academy50Quest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public Academy50Quest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public Academy50Quest(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

            GameNpc[] npcs = WorldMgr.GetNPCsByName("Master Ferowl", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 559690 && npc.Y == 510258)
					{
						Ferowl = npc;
						break;
					}

			if (Ferowl == null)
			{
				Ferowl = new GameNpc();
				Ferowl.Model = 61;
				Ferowl.Name = "Master Ferowl";
				if (log.IsWarnEnabled)
					log.Warn("Could not find " + Ferowl.Name + " , creating it ...");
				Ferowl.GuildName = "";
				Ferowl.Realm = ERealm.Albion;
				Ferowl.CurrentRegionID = 1;
				Ferowl.Size = 51;
				Ferowl.Level = 40;
				Ferowl.X = 559461;
				Ferowl.Y = 510653;
				Ferowl.Z = 2712;
				Ferowl.Heading = 3607;
				Ferowl.AddToWorld();

				if (SAVE_INTO_DATABASE)
					Ferowl.SaveIntoDatabase();
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Morgana", ERealm.None);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 306093 && npc.Y == 670120)
					{
						Morgana = npc as Morgana;
						break;
					}

			if (Morgana == null)
			{
				Morgana = new Morgana();
				Morgana.Model = 283;
				Morgana.Name = "Morgana";
				if (log.IsWarnEnabled)
					log.Warn("Could not find " + Morgana.Name + " , creating it ...");
				Morgana.GuildName = "";
				Morgana.Realm = ERealm.None;
				Morgana.CurrentRegionID = 1;
				Morgana.Size = 51;
				Morgana.Level = 90;
				Morgana.X = 306093;
				Morgana.Y = 670120;
				Morgana.Z = 3116;
				Morgana.Heading = 3277;

				MorganaBrain brain = new MorganaBrain();
				brain.AggroLevel = 0;
				brain.AggroRange = 0;
				Morgana.SetOwnBrain(brain);

				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 98, 43);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 133, 61);
				Morgana.Inventory = template.CloseTemplate();

//				Morgana.AddNPCEquipment((byte) eVisibleItems.TORSO, 98, 43, 0, 0);
//				Morgana.AddNPCEquipment((byte) eVisibleItems.BOOT, 133, 61, 0, 0);

				Morgana.AddToWorld();
				if (SAVE_INTO_DATABASE)
					Morgana.SaveIntoDatabase();
			}
			// end npc

		/*	npcs = WorldMgr.GetNPCsByName("Bechard", eRealm.None);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 306025 && npc.Y == 670473)
					{
						Bechard = npc as Bechard;
						break;
					}

			if (Bechard == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bechard , creating it ...");
				Bechard = new Bechard();
				Bechard.Model = 606;
				Bechard.Name = "Bechard";
				Bechard.GuildName = "";
				Bechard.Realm = eRealm.None;
				Bechard.CurrentRegionID = 1;
				Bechard.Size = 50;
				Bechard.Level = 63;
				Bechard.X = 306025;
				Bechard.Y = 670473;
				Bechard.Z = 2863;
				Bechard.Heading = 3754;
				Bechard.AddToWorld();

				if (SAVE_INTO_DATABASE)
					Bechard.SaveIntoDatabase();
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Silcharde", eRealm.None);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 306252 && npc.Y == 670274)
					{
						Silcharde = npc as Silcharde;
						break;
					}

			if (Silcharde == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Silcharde , creating it ...");
				Silcharde = new Silcharde();
				Silcharde.Model = 606;
				Silcharde.Name = "Silcharde";
				Silcharde.GuildName = "";
				Silcharde.Realm = eRealm.None;
				Silcharde.CurrentRegionID = 1;
				Silcharde.Size = 50;
				Silcharde.Level = 63;
				Silcharde.X = 306252;
				Silcharde.Y = 670274;
				Silcharde.Z = 2857;
				Silcharde.Heading = 3299;
				Silcharde.AddToWorld();

				if (SAVE_INTO_DATABASE)
					Silcharde.SaveIntoDatabase();
			}
			// end npc*/

			#endregion

			#region Item Declarations

			sealed_pouch = GameServer.Database.FindObjectByKey<DbItemTemplates>("sealed_pouch");
			if (sealed_pouch == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sealed Pouch , creating it ...");
				sealed_pouch = new DbItemTemplates();
				sealed_pouch.Id_nb = "sealed_pouch";
				sealed_pouch.Name = "Sealed Pouch";
				sealed_pouch.Level = 8;
				sealed_pouch.Item_Type = 29;
				sealed_pouch.Model = 488;
				sealed_pouch.IsDropable = true;
				sealed_pouch.IsPickable = true;
				sealed_pouch.DPS_AF = 0;
				sealed_pouch.SPD_ABS = 0;
				sealed_pouch.Object_Type = 41;
				sealed_pouch.Hand = 0;
				sealed_pouch.Type_Damage = 0;
				sealed_pouch.Quality = 100;
				sealed_pouch.Weight = 12;

				
					GameServer.Database.AddObject(sealed_pouch);
			}
// end item

			DbItemTemplates item = null;

			WizardEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicBoots");
			if (WizardEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizards Epic Boots , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicBoots";
				item.Name = "Bernor's Numinous Boots";
				item.Level = 50;
				item.Item_Type = 23;
				item.Model = 143;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Skill_Cold;

				item.Bonus2 = 22;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Body;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Energy;

				
					GameServer.Database.AddObject(item);

				WizardEpicBoots = item;
			}
//end item
			//Bernor's Numinous Coif 
			WizardEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicHelm");
			if (WizardEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizards Epic Helm , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicHelm";
				item.Name = "Bernor's Numinous Cap";
				item.Level = 50;
				item.Item_Type = 21;
				item.Model = 1290; //NEED TO WORK ON..
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 13;
				item.Bonus1Type = (int) EStat.DEX;

				item.Bonus2 = 21;
				item.Bonus2Type = (int) EStat.INT;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Thrust;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Spirit;

				
					GameServer.Database.AddObject(item);

				WizardEpicHelm = item;
			}
//end item
			//Bernor's Numinous Gloves 
			WizardEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicGloves");
			if (WizardEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizards Epic Gloves , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicGloves";
				item.Name = "Bernor's Numinous Gloves ";
				item.Level = 50;
				item.Item_Type = 22;
				item.Model = 142;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 16;
				item.Bonus1Type = (int) EStat.DEX;

				item.Bonus2 = 18;
				item.Bonus2Type = (int) EStat.INT;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Matter;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Heat;

				
					GameServer.Database.AddObject(item);

				WizardEpicGloves = item;
			}

			//Bernor's Numinous Hauberk 
			WizardEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicVest");
			if (WizardEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizards Epic Vest , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicVest";
				item.Name = "Bernor's Numinous Robes";
				item.Level = 50;
				item.Item_Type = 25;
				item.Model = 798;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EResist.Cold;

				item.Bonus2 = 14;
				item.Bonus2Type = (int) EProperty.PowerRegenerationRate;

				item.Bonus3 = 24;
				item.Bonus3Type = (int) EProperty.MaxHealth;

				
					GameServer.Database.AddObject(item);

				WizardEpicVest = item;

			}
			//Bernor's Numinous Legs 
			WizardEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicLegs");
			if (WizardEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizards Epic Legs , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicLegs";
				item.Name = "Bernor's Numinous Pants";
				item.Level = 50;
				item.Item_Type = 27;
				item.Model = 140;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Skill_Fire;

				item.Bonus2 = 8;
				item.Bonus2Type = (int) EResist.Cold;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Energy;

				
					GameServer.Database.AddObject(item);

				WizardEpicLegs = item;

			}
			//Bernor's Numinous Sleeves 
			WizardEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("WizardEpicArms");
			if (WizardEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Wizard Epic Arms , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "WizardEpicArms";
				item.Name = "Bernor's Numinous Sleeves";
				item.Level = 50;
				item.Item_Type = 28;
				item.Model = 141;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Skill_Earth;

				item.Bonus2 = 18;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 16;
				item.Bonus3Type = (int) EStat.INT;

				
					GameServer.Database.AddObject(item);

				WizardEpicArms = item;

			}
//Minstrel Epic Sleeves End
			MinstrelEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicBoots");
			if (MinstrelEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrels Epic Boots , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicBoots";
				item.Name = "Boots of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 23;
				item.Model = 727;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 7;
				item.Bonus1Type = (int) EStat.DEX;

				item.Bonus2 = 27;
				item.Bonus2Type = (int) EStat.QUI;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Slash;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Cold;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicBoots = item;

			}
//end item
			//of Coruscating Harmony  Coif 
			MinstrelEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicHelm");
			if (MinstrelEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrels Epic Helm , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicHelm";
				item.Name = "Coif of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 21;
				item.Model = 1290; //NEED TO WORK ON..
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 16;
				item.Bonus1Type = (int) EStat.CON;

				item.Bonus2 = 18;
				item.Bonus2Type = (int) EStat.CHR;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Thrust;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Energy;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicHelm = item;

			}
//end item
			//of Coruscating Harmony  Gloves 
			MinstrelEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicGloves");
			if (MinstrelEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrels Epic Gloves , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicGloves";
				item.Name = "Gauntlets of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 22;
				item.Model = 726;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 15;
				item.Bonus1Type = (int) EStat.CON;

				item.Bonus2 = 19;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Crush;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Heat;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicGloves = item;

			}
			//of Coruscating Harmony  Hauberk 
			MinstrelEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicVest");
			if (MinstrelEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrels Epic Vest , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicVest";
				item.Name = "Habergeon of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 25;
				item.Model = 723;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 6;
				item.Bonus1Type = (int) EResist.Cold;

				item.Bonus2 = 8;
				item.Bonus2Type = (int) EProperty.PowerRegenerationRate;

				item.Bonus3 = 39;
				item.Bonus3Type = (int) EProperty.MaxHealth;

				item.Bonus4 = 6;
				item.Bonus4Type = (int) EResist.Energy;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicVest = item;

			}
			//of Coruscating Harmony  Legs 
			MinstrelEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicLegs");
			if (MinstrelEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrels Epic Legs , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicLegs";
				item.Name = "Chaussess of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 27;
				item.Model = 724;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 15;
				item.Bonus1Type = (int) EStat.STR;

				item.Bonus2 = 19;
				item.Bonus2Type = (int) EStat.CON;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Body;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Heat;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicLegs = item;

			}
			//of Coruscating Harmony  Sleeves 
			MinstrelEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("MinstrelEpicArms");
			if (MinstrelEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Minstrel Epic Arms , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "MinstrelEpicArms";
				item.Name = "Sleeves of Coruscating Harmony";
				item.Level = 50;
				item.Item_Type = 28;
				item.Model = 725;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 100;
				item.SPD_ABS = 27;
				item.Object_Type = 35;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 16;
				item.Bonus1Type = (int) EStat.STR;

				item.Bonus2 = 21;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Crush;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Body;

				
					GameServer.Database.AddObject(item);

				MinstrelEpicArms = item;
			}

			SorcerorEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicBoots");
			if (SorcerorEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Boots , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicBoots";
				item.Name = "Boots of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 23;
				item.Model = 143;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Focus_Matter;

				item.Bonus2 = 22;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Matter;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Energy;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicBoots = item;

			}
//end item
			//of Mental Acuity Coif 
			SorcerorEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicHelm");
			if (SorcerorEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Helm , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicHelm";
				item.Name = "Cap of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 21;
				item.Model = 1290; //NEED TO WORK ON..
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 13;
				item.Bonus1Type = (int) EStat.DEX;

				item.Bonus2 = 21;
				item.Bonus2Type = (int) EStat.INT;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Slash;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Thrust;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicHelm = item;

			}
//end item
			//of Mental Acuity Gloves 
			SorcerorEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicGloves");
			if (SorcerorEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Gloves , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicGloves";
				item.Name = "Gloves of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 22;
				item.Model = 142;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 16;
				item.Bonus1Type = (int) EStat.DEX;

				item.Bonus2 = 18;
				item.Bonus2Type = (int) EStat.INT;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Cold;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Heat;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicGloves = item;

			}
			//of Mental Acuity Hauberk 
			SorcerorEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicVest");
			if (SorcerorEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Vest , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicVest";
				item.Name = "Vest of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 25;
				item.Model = 804;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EResist.Spirit;

				item.Bonus2 = 14;
				item.Bonus2Type = (int) EProperty.PowerRegenerationRate;

				item.Bonus3 = 24;
				item.Bonus3Type = (int) EProperty.MaxHealth;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicVest = item;

			}
			//of Mental Acuity Legs 
			SorcerorEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicLegs");
			if (SorcerorEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Legs , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicLegs";
				item.Name = "Pants of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 27;
				item.Model = 140;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Focus_Mind;

				item.Bonus2 = 19;
				item.Bonus2Type = (int) EStat.CON;

				item.Bonus3 = 8;
				item.Bonus3Type = (int) EResist.Body;

				item.Bonus4 = 8;
				item.Bonus4Type = (int) EResist.Spirit;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicLegs = item;

			}
			//of Mental Acuity Sleeves 
			SorcerorEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("SorcerorEpicArms");
			if (SorcerorEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sorceror Epic Arms , creating it ...");
				item = new DbItemTemplates();
				item.Id_nb = "SorcerorEpicArms";
				item.Name = "Sleeves of Mental Acuity";
				item.Level = 50;
				item.Item_Type = 28;
				item.Model = 141;
				item.IsDropable = true;
				item.IsPickable = true;
				item.DPS_AF = 50;
				item.SPD_ABS = 0;
				item.Object_Type = 32;
				item.Quality = 100;
				item.Weight = 22;
				item.Bonus = 35;
				item.MaxCondition = 50000;
				item.MaxDurability = 50000;
				item.Condition = 50000;
				item.Durability = 50000;

				item.Bonus1 = 4;
				item.Bonus1Type = (int) EProperty.Focus_Body;

				item.Bonus2 = 16;
				item.Bonus2Type = (int) EStat.DEX;

				item.Bonus3 = 18;
				item.Bonus3Type = (int) EStat.INT;

				
					GameServer.Database.AddObject(item);

				SorcerorEpicArms = item;
			}
			//Item Descriptions End

			#endregion

			//morganaArea = WorldMgr.GetRegion(Morgana.CurrentRegionID).AddArea(new Area.Circle(null, Morgana.X, Morgana.Y, 0, 1000));
			//morganaArea.RegisterPlayerEnter(new DOLEventHandler(PlayerEnterMorganaArea));

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Ferowl, GameObjectEvent.Interact, new CoreEventHandler(TalkToFerowl));
			GameEventMgr.AddHandler(Ferowl, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToFerowl));

			/* Now we bring to Ferowl the possibility to give this quest to players */
			Ferowl.AddQuestToGive(typeof (Academy50Quest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			//if not loaded, don't worry
			if (Ferowl == null)
				return;

			//morganaArea.UnRegisterPlayerEnter(new DOLEventHandler(PlayerEnterMorganaArea));
			//WorldMgr.GetRegion(Morgana.CurrentRegionID).RemoveArea(morganaArea);
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Ferowl, GameObjectEvent.Interact, new CoreEventHandler(TalkToFerowl));
			GameEventMgr.RemoveHandler(Ferowl, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToFerowl));

			/* Now we remove to Ferowl the possibility to give this quest to players */
			Ferowl.RemoveQuestToGive(typeof (Academy50Quest));
		}

		/*protected static void PlayerEnterMorganaArea(DOLEvent e, object sender, EventArgs args)
		{
			AreaEventArgs aargs = args as AreaEventArgs;
			GamePlayer player = aargs.GameObject as GamePlayer;
			Academy_50 quest = player.IsDoingQuest(typeof (Academy_50)) as Academy_50;

			if (quest != null && Morgana.ObjectState != GameObject.eObjectState.Active)
			{
				// player near grove
				//SendSystemMessage(player, "As you approach the fallen tower you see Morgana standing on top of the tower.");
				quest.CreateMorgana();

				//if (player.Group != null)
					//Morgana.Yell("Ha, is this all the forces of Albion have to offer? I expected a whole army leaded by my brother Arthur, but what do they send a little group of adventurers lead by a poor " + player.CharacterClass.Name + "?");
				//else
					//Morgana.Yell("Ha, is this all the forces of Albion have to offer? I expected a whole army leaded by my brother Arthur, but what do they send a poor " + player.CharacterClass.Name + "?");

				foreach (GamePlayer visPlayer in Morgana.GetPlayersInRadius(WorldMgr.VISIBILITY_DISTANCE))
				{
					visPlayer.Out.SendSpellCastAnimation(Morgana, 1, 20);
				}
			}
		}*/

		/*protected virtual void CreateMorgana()
		{
			if (Morgana == null)
			{
				Morgana = new Morgana();
				Morgana.Model = 283;
				Morgana.Name = "Morgana";
				if (log.IsWarnEnabled)
					log.Warn("Could not find " + Morgana.Name + " , creating it ...");
				Morgana.GuildName = "";
				Morgana.Realm = eRealm.None;
				Morgana.CurrentRegionID = 1;
				Morgana.Size = 51;
				Morgana.Level = 90;
				Morgana.X = 306056;
				Morgana.Y = 670106;
				Morgana.Z = 3095;
				Morgana.Heading = 3261;

				
				StandardMobBrain brain = new StandardMobBrain();
				brain.AggroLevel = 0;
				brain.AggroRange = 0;
				Morgana.SetOwnBrain(brain);

				GameNpcInventoryTemplate template = new GameNpcInventoryTemplate();
				template.AddNPCEquipment(eInventorySlot.TorsoArmor, 98, 43);
				template.AddNPCEquipment(eInventorySlot.FeetArmor, 133, 61);
				Morgana.Inventory = template.CloseTemplate();

//				Morgana.AddNPCEquipment((byte) eVisibleItems.TORSO, 98, 43, 0, 0);
//				Morgana.AddNPCEquipment((byte) eVisibleItems.BOOT, 133, 61, 0, 0);
			}

			Morgana.AddToWorld();
		}

		protected virtual void DeleteMorgana()
		{
			if (Morgana != null && MorganaBrain.CanRemoveMorgana)
				Morgana.RemoveFromWorld();
		}*/

		protected static void TalkToFerowl(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Ferowl.CanGiveQuest(typeof (Academy50Quest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			Academy50Quest quest = player.IsDoingQuest(typeof (Academy50Quest)) as Academy50Quest;

			if (e == GameObjectEvent.Interact)
			{
				// Nag to finish quest
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Ferowl.SayTo(player, "Albions fate lies in you hands. Seek out [Morgana] at the fallen tower in Lyonesse!");
							break;
						case 2:
							Ferowl.SayTo(player, "Were you able to [fulfill] your given task? Albions fate lies in you hands. ");
							break;
					}
					
				}
				else
				{
					Ferowl.SayTo(player, "Ah good to see you, there are rumors about your tasks all over Albion, yet we are in need of your [services] once again!");
				}
				return;
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "services":
							player.Out.SendQuestSubscribeCommand(Ferowl, QuestMgr.GetIDForQuestType(typeof(Academy50Quest)), "Will you help Ferowl [Academy Level 50 Epic]");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "Morgana":
							Ferowl.SayTo(player, "You must have heard about her, she's the evil sister of King Arthur, tried to take his throne. She must be [stopped]!");
							break;
						case "stopped":
							Ferowl.SayTo(player, "Once Morgana has summoned her army everything is lost. So hurry and stop her unholy rituals. With the help of two mighty demons Silcharde and Bechard she can summon as many minions as she wants. Killing them should be enough to stop her [ritual].");
							break;
						case "ritual":
							Ferowl.SayTo(player, "Morgana is probably performing her rital at the fallen tower in Lyonesse. To get there follow the Telamon road past the majority of the Danaoian Farmers, until you see the [fallen tower].");
							break;
						case "fallen tower":
							Ferowl.SayTo(player, "Be wise and don't take any unneccessary risks by going directly on Morgana , you might be a strong " + player.CharacterClass.Name + ", but you are no match for Morgana herself. Kill her demons and return to me, we will then try to take care of the rest, once her time has come.");
							break;

							// once the deomns are dead:
						case "fulfill":
							Ferowl.SayTo(player, "Did you find anything near the fallen tower? If yes [give it to me], we could need any hints we can get on our crusade against Morgana.");
							break;
						case "give it to me":
							if (quest.Step == 2)
							{
								RemoveItem(player, sealed_pouch);
								if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
									    eInventorySlot.LastBackpack))
								{
									Ferowl.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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
					if (rArgs.Item.Id_nb == sealed_pouch.Id_nb && quest.Step == 2)
					{
						if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
							eInventorySlot.LastBackpack))
						{
							Ferowl.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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
			if (player.IsDoingQuest(typeof (Academy50Quest)) != null)
				return true;

			if (player.CharacterClass.ID != (byte) ECharacterClass.Minstrel &&
				player.CharacterClass.ID != (byte) ECharacterClass.Wizard &&
				player.CharacterClass.ID != (byte) ECharacterClass.Sorcerer)
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

		protected static void SubscribeQuest(CoreEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(Academy50Quest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAbortQuest(GamePlayer player, byte response)
		{
			Academy50Quest quest = player.IsDoingQuest(typeof (Academy50Quest)) as Academy50Quest;

			if (quest == null)
				return;

			if (response == 0x00)
			{
				SendSystemMessage(player, "Good, now go out there and finish your work!");
			}
			else
			{
				SendSystemMessage(player, "Aborting Quest " + questTitle + ". You can start over again if you want.");
				quest.AbortQuest();
			}
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Ferowl.CanGiveQuest(typeof (Academy50Quest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (Academy50Quest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				// Check to see if we can add quest
				if (!Ferowl.GiveQuest(typeof (Academy50Quest), player, 1))
					return;

				Ferowl.SayTo(player, "I have heard rumors about the witch [Morgana] trying to summon an army of demons to crush the mighty city of Camelot!");
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "Symbol of the Broken (Level 50 Academy Epic)"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Seek out Bechard and Silcharde at the fallen tower in Lyonesse and kill them with rest of summoned demons!\n" +
							"Bechard killed: ("+_BechardKilled+" | 1)\n" +
							"Silcharde killed: (" + _SilchardeKilled + " | 1)\n" +
							"Summoned demons killed: (" + _DemonicMinionsKilled + " | 20)";
					case 2:
						return "Return the pouch to Ferowl for your reward!";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (Academy50Quest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs != null && gArgs.Target != null)
				{
					if(gArgs.Target is DemonicMinion && _DemonicMinionsKilled <= 20)
                    {
						_DemonicMinionsKilled++;
						player.Out.SendQuestUpdate(this);
					}
					if (gArgs.Target is Bechard && _BechardKilled <= 1)
					{
						_BechardKilled++;
						player.Out.SendQuestUpdate(this);
					}
					if (gArgs.Target is Silcharde && _SilchardeKilled <= 1)
					{
						_SilchardeKilled++;
						player.Out.SendQuestUpdate(this);
					}				
					if (_BechardKilled >= 1 && _SilchardeKilled >= 1 && _DemonicMinionsKilled >= 20 )
					{
						Morgana.Yell("You may have stopped me here, but I'll come back! Albion will be mine!");
						//DeleteMorgana();
						player.Out.SendMessage("A sense of calm settles about you!", EChatType.CT_Broadcast, EChatLoc.CL_ChatWindow);
						GiveItem(player, sealed_pouch);
						m_questPlayer.Out.SendMessage("Take the pouch to " + Ferowl.GetName(0, true), EChatType.CT_System, EChatLoc.CL_SystemWindow);
						Step = 2;
					}
				}
			}
			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Ferowl.Name && gArgs.Item.Id_nb == sealed_pouch.Id_nb)
				{
					if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
						    eInventorySlot.LastBackpack))
					{
						Ferowl.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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

			RemoveItem(m_questPlayer, sealed_pouch, false);
		}

		public override void FinishQuest()
		{
			RemoveItem(Ferowl, m_questPlayer, sealed_pouch);

			if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Minstrel)
			{
				GiveItem(m_questPlayer, MinstrelEpicBoots);
				GiveItem(m_questPlayer, MinstrelEpicHelm);
				GiveItem(m_questPlayer, MinstrelEpicGloves);
				GiveItem(m_questPlayer, MinstrelEpicArms);
				GiveItem(m_questPlayer, MinstrelEpicVest);
				GiveItem(m_questPlayer, MinstrelEpicLegs);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Wizard)
			{
				GiveItem(m_questPlayer, WizardEpicBoots);
				GiveItem(m_questPlayer, WizardEpicHelm);
				GiveItem(m_questPlayer, WizardEpicGloves);
				GiveItem(m_questPlayer, WizardEpicVest);
				GiveItem(m_questPlayer, WizardEpicArms);
				GiveItem(m_questPlayer, WizardEpicLegs);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Sorcerer)
			{
				GiveItem(m_questPlayer, SorcerorEpicBoots);
				GiveItem(m_questPlayer, SorcerorEpicHelm);
				GiveItem(m_questPlayer, SorcerorEpicGloves);
				GiveItem(m_questPlayer, SorcerorEpicVest);
				GiveItem(m_questPlayer, SorcerorEpicArms);
				GiveItem(m_questPlayer, SorcerorEpicLegs);
			}

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...


			m_questPlayer.GainExperience(EXpSource.Quest, 1937768448, false);
			//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
		}

		#region Allakhazam Epic Source

		/*
        * Return to Esmond in Cornwall Station once you reach level 50. If you had given him the dagger at the end of the level 48 epic, he will ask you if you want it back. If he asks, accept the knife back and continue. If not, make sure you have the ritual dagger with you. 
		* Go to Lyonesse and find the tower, which is located at 20k, 39k. You can simply follow the Telamon road past the majority of the Danaoian Farmers, until you see a fallen tower with two large named demons (purple to 50) and Morgana sitting on top of the tower. 
		* To defeat them is quite easy and can take as little as 6 people. As long as you have at least one tank, a healer, and someone who can root or mez, you should be ok. 
		* Do not attack Morgana. She will not do anything during this attack. Have someone root or mez one of the named demons, while the tank(s) hold aggro on the second one. When the aggroed one is defeated, a large group of tiny demons will appear and fly around the tower (they were all green to a 50). Take care of the previously rooted/mezed demon and another group of tiny demons will appear. Morgana will spout off something that can be heard across the zone, then leave. Kill all the tiny demons that remain. 
		* Once all the aggro has been cleared, stand next to the tower. There will be a message that says, "You sense the tower is clear of necromantic ties!" about 5 or so times. Your dagger should dissapear from your inventory, followed by a message that says, "A sense of calm settles about you!" When you recieve that message, your journal will update and tell you go to meet Master Ferowl again. 
		* Master Ferowl congratulates you on a job well done and asks you to go meet your trainer in Camelot for your reward. Also, Ferowl gives you 1,937,768,448 experience for some reason. 
		* Your trainer in Camelot should give you your epic armor, with another congratulations. 
		* The description of this quest was done by a Wizard. Other Academy classes might be slightly different. Also, this quest takes into consideration that you gave the knife to Esmond at the end of the 48 epic quest, which may or may not be a big deal.
        */

		#endregion
	}
}