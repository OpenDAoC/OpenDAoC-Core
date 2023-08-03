using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Albion
{
	public class Defenders50Quest : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Feast of the Decadent";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;

		private static GameNpc Lidmann = null; // Start NPC
		private static EpicCailleachUragaig Uragaig = null; // Mob to kill

		private static DbItemTemplates sealed_pouch = null; //sealed pouch
		private static DbItemTemplates ScoutEpicBoots = null; //Brigandine of Vigilant Defense  Boots 
		private static DbItemTemplates ScoutEpicHelm = null; //Brigandine of Vigilant Defense  Coif 
		private static DbItemTemplates ScoutEpicGloves = null; //Brigandine of Vigilant Defense  Gloves 
		private static DbItemTemplates ScoutEpicVest = null; //Brigandine of Vigilant Defense  Hauberk 
		private static DbItemTemplates ScoutEpicLegs = null; //Brigandine of Vigilant Defense  Legs 
		private static DbItemTemplates ScoutEpicArms = null; //Brigandine of Vigilant Defense  Sleeves 
		private static DbItemTemplates ArmsmanEpicBoots = null; //Shadow Shrouded Boots 
		private static DbItemTemplates ArmsmanEpicHelm = null; //Shadow Shrouded Coif 
		private static DbItemTemplates ArmsmanEpicGloves = null; //Shadow Shrouded Gloves 
		private static DbItemTemplates ArmsmanEpicVest = null; //Shadow Shrouded Hauberk 
		private static DbItemTemplates ArmsmanEpicLegs = null; //Shadow Shrouded Legs 
		private static DbItemTemplates ArmsmanEpicArms = null; //Shadow Shrouded Sleeves 
		private static DbItemTemplates TheurgistEpicBoots = null; //Valhalla Touched Boots 
		private static DbItemTemplates TheurgistEpicHelm = null; //Valhalla Touched Coif 
		private static DbItemTemplates TheurgistEpicGloves = null; //Valhalla Touched Gloves 
		private static DbItemTemplates TheurgistEpicVest = null; //Valhalla Touched Hauberk 
		private static DbItemTemplates TheurgistEpicLegs = null; //Valhalla Touched Legs 
		private static DbItemTemplates TheurgistEpicArms = null; //Valhalla Touched Sleeves 
		private static DbItemTemplates FriarEpicBoots = null; //Subterranean Boots 
		private static DbItemTemplates FriarEpicHelm = null; //Subterranean Coif 
		private static DbItemTemplates FriarEpicGloves = null; //Subterranean Gloves 
		private static DbItemTemplates FriarEpicVest = null; //Subterranean Hauberk 
		private static DbItemTemplates FriarEpicLegs = null; //Subterranean Legs
		private static DbItemTemplates FriarEpicArms = null; //Subterranean Sleeves
		private static DbItemTemplates MaulerAlbEpicBoots = null;
		private static DbItemTemplates MaulerAlbEpicHelm = null;
		private static DbItemTemplates MaulerAlbEpicGloves = null;
		private static DbItemTemplates MaulerAlbEpicVest = null;
		private static DbItemTemplates MaulerAlbEpicLegs = null;
		private static DbItemTemplates MaulerAlbEpicArms = null;

		// Constructors
		public Defenders50Quest() : base()
		{
		}

		public Defenders50Quest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public Defenders50Quest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public Defenders50Quest(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Lidmann Halsey", ERealm.Albion);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 466464 && npc.Y == 634554)
					{
						Lidmann = npc;
						break;
					}

			if (Lidmann == null)
			{

				Lidmann = new GameNpc();
				Lidmann.Model = 64;
				Lidmann.Name = "Lidmann Halsey";

				if (log.IsWarnEnabled)
					log.Warn("Could not find " + Lidmann.Name + ", creating it ...");

				Lidmann.GuildName = "";
				Lidmann.Realm = ERealm.Albion;
				Lidmann.CurrentRegionID = 1;
				Lidmann.Size = 50;
				Lidmann.Level = 50;
				Lidmann.X = 466464;
				Lidmann.Y = 634554;
				Lidmann.Z = 1954;
				Lidmann.Heading = 1809;
				Lidmann.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Lidmann.SaveIntoDatabase();
				}

			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Cailleach Uragaig", ERealm.None);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 1 && npc.X == 316218 && npc.Y == 664484)
					{
						Uragaig = npc as EpicCailleachUragaig;
						break;
					}

			if (Uragaig == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Uragaig , creating it ...");
				Uragaig = new EpicCailleachUragaig();
				Uragaig.Model = 349;
				Uragaig.Name = "Cailleach Uragaig";
				Uragaig.GuildName = "";
				Uragaig.Realm = ERealm.None;
				Uragaig.CurrentRegionID = 1;
				Uragaig.Size = 55;
				Uragaig.Level = 70;
				Uragaig.X = 316218;
				Uragaig.Y = 664484;
				Uragaig.Z = 2736;
				Uragaig.Heading = 3072;
				Uragaig.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Uragaig.SaveIntoDatabase();
				}

			}
			// end npc

			#endregion

			#region defineItems

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
				sealed_pouch.IsDropable = false;
				sealed_pouch.IsPickable = false;
				sealed_pouch.DPS_AF = 0;
				sealed_pouch.SPD_ABS = 0;
				sealed_pouch.Object_Type = 41;
				sealed_pouch.Hand = 0;
				sealed_pouch.Type_Damage = 0;
				sealed_pouch.Quality = 100;
				sealed_pouch.Weight = 12;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(sealed_pouch);
				}

			}
			// end item
			DbItemTemplates i = null;
			ScoutEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicBoots");
			if (ScoutEpicBoots == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicBoots";
				i.Name = "Brigandine Boots of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 731;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +10, Dex +18, Qui +15, Spirit +8%
				i.Bonus1 = 10;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 15;
				i.Bonus3Type = (int)EStat.QUI;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Spirit;
				{
					GameServer.Database.AddObject(i);
				}
				ScoutEpicBoots = i;

			}
			//end item
			//Brigandine of Vigilant Defense  Coif
			ScoutEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicHelm");
			if (ScoutEpicHelm == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicHelm";
				i.Name = "Brigandine Coif of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Dex +12, Qui +22, Crush +8%, Heat +8%
				i.Bonus1 = 12;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 22;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Crush;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Heat;
				{
					GameServer.Database.AddObject(i);
				}

				ScoutEpicHelm = i;

			}
			//end item
			//Brigandine of Vigilant Defense  Gloves
			ScoutEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicGloves");
			if (ScoutEpicGloves == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicGloves";
				i.Name = "Brigandine Gloves of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 732;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Dex +21, Longbow +5, Body +8%, Slash +8%
				i.Bonus1 = 21;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 5;
				i.Bonus2Type = (int)EProperty.Skill_Long_bows;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Slash;

				{
					GameServer.Database.AddObject(i);
				}

				ScoutEpicGloves = i;

			}
			//Brigandine of Vigilant Defense  Hauberk
			ScoutEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicVest");
			if (ScoutEpicVest == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicVest";
				i.Name = "Brigandine Jerkin of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 728;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Str +18, HP +45, Spirit +4%, Thrust +4%
				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EResist.Thrust;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Spirit;

				i.Bonus4 = 45;
				i.Bonus4Type = (int)EProperty.MaxHealth;
				{
					GameServer.Database.AddObject(i);
				}

				ScoutEpicVest = i;

			}
			//Brigandine of Vigilant Defense  Legs
			ScoutEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicLegs");
			if (ScoutEpicLegs == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicLegs";
				i.Name = "Brigandine Legs of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 729;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +22, Dex +15, Qui +7, Spirit +6%
				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 7;
				i.Bonus3Type = (int)EStat.QUI;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Spirit;
				{
					GameServer.Database.AddObject(i);
				}
				ScoutEpicLegs = i;

			}
			//Brigandine of Vigilant Defense  Sleeves
			ScoutEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("ScoutEpicArms");
			if (ScoutEpicArms == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ScoutEpicArms";
				i.Name = "Brigandine Sleeves of Vigilant Defense";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 730;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 19;
				i.Object_Type = 34;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +22, Str +18, Energy +8%, Slash +4%
				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Energy;

				i.Bonus4 = 4;
				i.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(i);
				}

				ScoutEpicArms = i;

			}
			//Scout Epic Sleeves End

			//Armsman Epic Boots Start
			ArmsmanEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicBoots");
			if (ArmsmanEpicBoots == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicBoots";
				i.Name = "Sabaton of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 692;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Str +15, Qui +15, Spirit +8%
				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Spirit;


				{
					GameServer.Database.AddObject(i);
				}
				ArmsmanEpicBoots = i;

			}
			//end item
			//of the Stalwart Arm Coif
			ArmsmanEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicHelm");
			if (ArmsmanEpicHelm == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicHelm";
				i.Name = "Coif of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +19, Qui +18, Body +6%, Crush +6%
				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 6;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Crush;
				{
					GameServer.Database.AddObject(i);
				}

				ArmsmanEpicHelm = i;

			}
			//end item
			//of the Stalwart Arm Gloves
			ArmsmanEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicGloves");
			if (ArmsmanEpicGloves == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicGloves";
				i.Name = "Gloves of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 691;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Str +22, Dex +15, Cold +6%, Slash +6
				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 6;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(i);
				}

				ArmsmanEpicGloves = i;

			}
			//of the Stalwart Arm Hauberk
			ArmsmanEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicVest");
			if (ArmsmanEpicVest == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicVest";
				i.Name = "Jerkin of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 688;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: HP +45, Str +18, Energy +4%, Slash +4%
				// there is an additional bonus here I couldn't figure out how to add
				// 3 charges of 75 point shield ???
				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EResist.Slash;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Energy;

				i.Bonus4 = 45;
				i.Bonus4Type = (int)EProperty.MaxHealth;

				{
					GameServer.Database.AddObject(i);
				}

				ArmsmanEpicVest = i;

			}
			//of the Stalwart Arm Legs
			ArmsmanEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicLegs");
			if (ArmsmanEpicLegs == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicLegs";
				i.Name = "Legs of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 689;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +24, Str +10, Matter +8%, Crush +8%
				i.Bonus1 = 24;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 10;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Matter;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Crush;
				{
					GameServer.Database.AddObject(i);
				}

				ArmsmanEpicLegs = i;

			}
			//of the Stalwart Arm Sleeves
			ArmsmanEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("ArmsmanEpicArms");
			if (ArmsmanEpicArms == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "ArmsmanEpicArms";
				i.Name = "Sleeves of the Stalwart Arm";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 690;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 34;
				i.Object_Type = 36;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +19, Dex +18, Heat +6%, Thrust +6%
				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 6;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				ArmsmanEpicArms = i;

			}
			FriarEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicBoots");
			if (FriarEpicBoots == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicBoots";
				i.Name = "Prayer-bound Boots";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 40;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Qui +18, Dex +15, Spirit +10%, Con +12
				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.QUI;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 12;
				i.Bonus3Type = (int)EStat.CON;

				i.Bonus4 = 10;
				i.Bonus4Type = (int)EResist.Spirit;
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicBoots = i;

			}
			//end item
			//Prayer-bound Coif
			FriarEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicHelm");
			if (FriarEpicHelm == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicHelm";
				i.Name = "Prayer-bound Coif";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Dex +15, Pie +12, Con +10, Enchantment +4
				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 12;
				i.Bonus2Type = (int)EStat.PIE;

				i.Bonus3 = 10;
				i.Bonus3Type = (int)EStat.CON;

				i.Bonus4 = 4;
				i.Bonus4Type = (int)EProperty.Skill_Enhancement; //guessing here
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicHelm = i;

			}
			//end item
			//Prayer-bound Gloves
			FriarEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicGloves");
			if (FriarEpicGloves == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicGloves";
				i.Name = "Prayer-bound Gloves";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 39;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Pie +15, Rejuvination +4, Qui +15, Crush +6%
				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.PIE;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 6;
				i.Bonus3Type = (int)EResist.Crush;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EProperty.Skill_Rejuvenation; //guessing here
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicGloves = i;

			}
			//Prayer-bound Hauberk
			FriarEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicVest");
			if (FriarEpicVest == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicVest";
				i.Name = "Prayer-bound Jerkin";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 797;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: HP +33, Pwr +10, Spirit 4%, Crush 6%
				// Charged (3 Max) Self-Only Shield -- 75 AF, Duration 10 mins (no clue how to add this)
				i.Bonus1 = 10;
				i.Bonus1Type = (int)EProperty.MaxMana;

				i.Bonus2 = 6;
				i.Bonus2Type = (int)EResist.Crush;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Spirit;

				i.Bonus4 = 33;
				i.Bonus4Type = (int)EProperty.MaxHealth;
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicVest = i;

			}
			//Prayer-bound Legs
			FriarEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicLegs");
			if (FriarEpicLegs == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicLegs";
				i.Name = "Prayer-bound Legs";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 37;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +22, Str +15, Heat +6%, Slash +6%
				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 22;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 6;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicLegs = i;

			}
			//Prayer-bound Sleeves
			FriarEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("FriarEpicArms");
			if (FriarEpicArms == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "FriarEpicArms";
				i.Name = "Prayer-bound Sleeves";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 38;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 10;
				i.Object_Type = 33;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Pie +18, Dex +16, Cold +8%, Thrust +8%
				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.PIE;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				FriarEpicArms = i;

			}
			TheurgistEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicBoots");
			if (TheurgistEpicBoots == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicBoots";
				i.Name = "Boots of Shielding Power";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 143;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Dex +16, Cold +6%, Body +8%, Energy +8%
				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 6;
				i.Bonus2Type = (int)EResist.Cold;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}

				TheurgistEpicBoots = i;

			}
			//end item
			//of Shielding Power Coif
			TheurgistEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicHelm");
			if (TheurgistEpicHelm == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicHelm";
				i.Name = "Coif of Shielding Power";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Int +21, Dex +13, Spirit +8%, Crush +8%
				i.Bonus1 = 21;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 13;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Spirit;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Crush;
				{
					GameServer.Database.AddObject(i);
				}

				TheurgistEpicHelm = i;

			}
			//end item
			//of Shielding Power Gloves
			TheurgistEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicGloves");
			if (TheurgistEpicGloves == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicGloves";
				i.Name = "Gloves of Shielding Power";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 142;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Dex +16, Int +18, Heat +8%, Matter +8%
				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.INT;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}

				TheurgistEpicGloves = i;

			}
			//of Shielding Power Hauberk
			TheurgistEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicVest");
			if (TheurgistEpicVest == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicVest";
				i.Name = "Jerkin of Shielding Power";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 733;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: HP +24, Power +14, Cold +4%,
				//triggered effect: Shield (3 charges max) duration 10 mins  (no clue how to implement)
				i.Bonus1 = 24;
				i.Bonus1Type = (int)EProperty.MaxHealth;

				i.Bonus2 = 14;
				i.Bonus2Type = (int)EProperty.MaxMana;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Cold;
				{
					GameServer.Database.AddObject(i);
				}

				TheurgistEpicVest = i;

			}
			//of Shielding Power Legs
			TheurgistEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicLegs");
			if (TheurgistEpicLegs == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicLegs";
				i.Name = "Legs of Shielding Power";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 140;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Con +19, Wind +4, Energy +10%, Cold +10%
				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Wind;

				i.Bonus3 = 10;
				i.Bonus3Type = (int)EResist.Energy;

				i.Bonus4 = 10;
				i.Bonus4Type = (int)EResist.Cold;
				{
					GameServer.Database.AddObject(i);
				}

				TheurgistEpicLegs = i;

			}
			//of Shielding Power Sleeves
			TheurgistEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("TheurgistEpicArms");
			if (TheurgistEpicArms == null)
			{
				i = new DbItemTemplates();
				i.Id_nb = "TheurgistEpicArms";
				i.Name = "Sleeves of Shielding Power";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 141;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 50;
				i.SPD_ABS = 0;
				i.Object_Type = 32;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;

				//bonuses: Int +18, Earth +4, Dex +16
				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Earth;

				i.Bonus3 = 16;
				i.Bonus3Type = (int)EStat.DEX;

				GameServer.Database.AddObject(i);

				TheurgistEpicArms = i;

			}

			MaulerAlbEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicBoots");
			MaulerAlbEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicHelm");
			MaulerAlbEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicGloves");
			MaulerAlbEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicVest");
			MaulerAlbEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicLegs");
			MaulerAlbEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("MaulerAlbEpicArms");
			//Item Descriptions End

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Lidmann, GameObjectEvent.Interact, new CoreEventHandler(TalkToLidmann));
			GameEventMgr.AddHandler(Lidmann, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLidmann));

			/* Now we bring to masterFrederick the possibility to give this quest to players */
			Lidmann.AddQuestToGive(typeof(Defenders50Quest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			//if not loaded, don't worry
			if (Lidmann == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Lidmann, GameObjectEvent.Interact, new CoreEventHandler(TalkToLidmann));
			GameEventMgr.RemoveHandler(Lidmann, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLidmann));

			/* Now we remove to masterFrederick the possibility to give this quest to players */
			Lidmann.RemoveQuestToGive(typeof (Defenders50Quest));
		}

		protected static void TalkToLidmann(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if (Lidmann.CanGiveQuest(typeof(Defenders50Quest), player) <= 0)
				return;

			// player is not allowed to start this quest until the quest rewards are available
			if (player.CharacterClass.ID == (byte)ECharacterClass.MaulerAlb &&
				(MaulerAlbEpicArms == null || MaulerAlbEpicBoots == null || MaulerAlbEpicGloves == null ||
				MaulerAlbEpicHelm == null || MaulerAlbEpicLegs == null || MaulerAlbEpicVest == null))
			{
				Lidmann.SayTo(player, "This quest is not available to Maulers yet.");
				return;
			}

			//We also check if the player is already doing the quest
			Defenders50Quest quest = player.IsDoingQuest(typeof (Defenders50Quest)) as Defenders50Quest;

			if (e == GameObjectEvent.Interact)
			{
				// Nag to finish quest
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Lidmann.SayTo(player, "Seek out Cailleach Uragaig in Lyonesse! Follow the road southwest into Lyonesse past the lesser telamon. " +
							                      "Keep going until you pass the two houses with the pikemen and pygmy goblins. " +
							                      "There is a clearing straight west where you see ruins of pillars. The cailleach sisterhood calls those ruins home.");
							break;
						case 2:
							Lidmann.SayTo(player, $"Hey ${player.Name}, did you [slay] Cailleach Uragaig?");
							break;
					}
				}
				else
				{
					// Check if player is qualifed for quest                
					Lidmann.SayTo(player, "Albion needs your [services].");
					
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
							player.Out.SendQuestSubscribeCommand(Lidmann, QuestMgr.GetIDForQuestType(typeof(Defenders50Quest)), "Will you help Lidmann [Defenders of Albion Level 50 Epic]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "slay":
							if (quest.Step == 2)
							{
								RemoveItem(player, sealed_pouch);
								if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
									    eInventorySlot.LastBackpack))
								{
									Lidmann.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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
							Lidmann.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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
			if (player.IsDoingQuest(typeof (Defenders50Quest)) != null)
				return true;

			if (player.CharacterClass.ID != (byte) ECharacterClass.Armsman &&
				player.CharacterClass.ID != (byte) ECharacterClass.Scout &&
				player.CharacterClass.ID != (byte) ECharacterClass.Theurgist &&
				player.CharacterClass.ID != (byte) ECharacterClass.Friar &&
				player.CharacterClass.ID != (byte) ECharacterClass.MaulerAlb)
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
			Defenders50Quest quest = player.IsDoingQuest(typeof (Defenders50Quest)) as Defenders50Quest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(Defenders50Quest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Lidmann.CanGiveQuest(typeof (Defenders50Quest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (Defenders50Quest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				// Check to see if we can add quest
				if (!Lidmann.GiveQuest(typeof (Defenders50Quest), player, 1))
					return;

				player.Out.SendMessage("Kill Cailleach Uragaig in Lyonesse!", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "Feast of the Decadent (Level 50 Defenders of Albion Epic)"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Seek out Cailleach Uragaig in Lyonesse and kill her!\n" +
						       "There is a clearing straight west in Lyonesse where you see ruins of pillars. " +
						       "The cailleach sisterhood calls those ruins home.";
					case 2:
						return "Give the sealed pouch to Lidmann Halsey at Adribard's Retreat.";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (Defenders50Quest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs != null && gArgs.Target != null && Uragaig != null)
				{
					if (gArgs.Target.Name == Uragaig.Name)
					{
						m_questPlayer.Out.SendMessage("Take the pouch to Lidmann Halsey", EChatType.CT_System, EChatLoc.CL_SystemWindow);
						GiveItem(player, sealed_pouch);
						Step = 2;
					}
				}
			}
			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Lidmann.Name && gArgs.Item.Id_nb == sealed_pouch.Id_nb)
				{
					if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
						    eInventorySlot.LastBackpack))
					{
						Lidmann.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
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
			RemoveItem(Lidmann, m_questPlayer, sealed_pouch);

			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Armsman)
			{
				GiveItem(m_questPlayer, ArmsmanEpicBoots);
				GiveItem(m_questPlayer, ArmsmanEpicArms);
				GiveItem(m_questPlayer, ArmsmanEpicGloves);
				GiveItem(m_questPlayer, ArmsmanEpicHelm);
				GiveItem(m_questPlayer, ArmsmanEpicLegs);
				GiveItem(m_questPlayer, ArmsmanEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Scout)
			{
				GiveItem(m_questPlayer, ScoutEpicArms);
				GiveItem(m_questPlayer, ScoutEpicBoots);
				GiveItem(m_questPlayer, ScoutEpicGloves);
				GiveItem(m_questPlayer, ScoutEpicHelm);
				GiveItem(m_questPlayer, ScoutEpicLegs);
				GiveItem(m_questPlayer, ScoutEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Theurgist)
			{
				GiveItem(m_questPlayer, TheurgistEpicArms);
				GiveItem(m_questPlayer, TheurgistEpicBoots);
				GiveItem(m_questPlayer, TheurgistEpicGloves);
				GiveItem(m_questPlayer, TheurgistEpicHelm);
				GiveItem(m_questPlayer, TheurgistEpicLegs);
				GiveItem(m_questPlayer, TheurgistEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.Friar)
			{
				GiveItem(m_questPlayer, FriarEpicArms);
				GiveItem(m_questPlayer, FriarEpicBoots);
				GiveItem(m_questPlayer, FriarEpicGloves);
				GiveItem(m_questPlayer, FriarEpicHelm);
				GiveItem(m_questPlayer, FriarEpicLegs);
				GiveItem(m_questPlayer, FriarEpicVest);
			}
			else if (m_questPlayer.CharacterClass.ID == (byte)ECharacterClass.MaulerAlb)
			{
				GiveItem(m_questPlayer, MaulerAlbEpicArms);
				GiveItem(m_questPlayer, MaulerAlbEpicBoots);
				GiveItem(m_questPlayer, MaulerAlbEpicGloves);
				GiveItem(m_questPlayer, MaulerAlbEpicHelm);
				GiveItem(m_questPlayer, MaulerAlbEpicLegs);
				GiveItem(m_questPlayer, MaulerAlbEpicVest);
			}

			m_questPlayer.GainExperience(EXpSource.Quest, 1937768448, true);
			//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
		}

		#region Allakhazam Epic Source

		/*
        *#25 talk to Lidmann
        *#26 seek out Loken in Raumarik Loc 47k, 25k, 4k, and kill him purp and 2 blue adds 
        *#27 return to Lidmann 
        *#28 give her the ball of flame
        *#29 talk with Lidmann about Loken�s demise
        *#30 go to MorlinCaan in Jordheim 
        *#31 give her the sealed pouch
        *#32 you get your epic armor as a reward
        */

		/*
            *Brigandine of Vigilant Defense  Boots 
            *Brigandine of Vigilant Defense  Coif
            *Brigandine of Vigilant Defense  Gloves
            *Brigandine of Vigilant Defense  Hauberk
            *Brigandine of Vigilant Defense  Legs
            *Brigandine of Vigilant Defense  Sleeves
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