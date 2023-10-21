using System;
using System.Reflection;
using Core.Database;
using Core.Database.Tables;
using Core.Events;
using Core.GS.PacketHandler;
using log4net;

namespace Core.GS.Quests.Albion
{
	public class ShadowsLvl50EpicAlbQuest : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Feast of the Decadent";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;

		private static GameNpc Lidmann = null; // Start NPC
		private static CailleachUragaig Uragaig = null; // Mob to kill

		private static DbItemTemplate sealed_pouch = null; //sealed pouch
		private static DbItemTemplate MercenaryEpicBoots = null; // of the Shadowy Embers  Boots 
		private static DbItemTemplate MercenaryEpicHelm = null; // of the Shadowy Embers  Coif 
		private static DbItemTemplate MercenaryEpicGloves = null; // of the Shadowy Embers  Gloves 
		private static DbItemTemplate MercenaryEpicVest = null; // of the Shadowy Embers  Hauberk 
		private static DbItemTemplate MercenaryEpicLegs = null; // of the Shadowy Embers  Legs 
		private static DbItemTemplate MercenaryEpicArms = null; // of the Shadowy Embers  Sleeves 
		private static DbItemTemplate ReaverEpicBoots = null; //Shadow Shrouded Boots 
		private static DbItemTemplate ReaverEpicHelm = null; //Shadow Shrouded Coif 
		private static DbItemTemplate ReaverEpicGloves = null; //Shadow Shrouded Gloves 
		private static DbItemTemplate ReaverEpicVest = null; //Shadow Shrouded Hauberk 
		private static DbItemTemplate ReaverEpicLegs = null; //Shadow Shrouded Legs 
		private static DbItemTemplate ReaverEpicArms = null; //Shadow Shrouded Sleeves 
		private static DbItemTemplate CabalistEpicBoots = null; //Valhalla Touched Boots 
		private static DbItemTemplate CabalistEpicHelm = null; //Valhalla Touched Coif 
		private static DbItemTemplate CabalistEpicGloves = null; //Valhalla Touched Gloves 
		private static DbItemTemplate CabalistEpicVest = null; //Valhalla Touched Hauberk 
		private static DbItemTemplate CabalistEpicLegs = null; //Valhalla Touched Legs 
		private static DbItemTemplate CabalistEpicArms = null; //Valhalla Touched Sleeves 
		private static DbItemTemplate InfiltratorEpicBoots = null; //Subterranean Boots 
		private static DbItemTemplate InfiltratorEpicHelm = null; //Subterranean Coif 
		private static DbItemTemplate InfiltratorEpicGloves = null; //Subterranean Gloves 
		private static DbItemTemplate InfiltratorEpicVest = null; //Subterranean Hauberk 
		private static DbItemTemplate InfiltratorEpicLegs = null; //Subterranean Legs 
		private static DbItemTemplate InfiltratorEpicArms = null; //Subterranean Sleeves		
		private static DbItemTemplate NecromancerEpicBoots = null; //Subterranean Boots 
		private static DbItemTemplate NecromancerEpicHelm = null; //Subterranean Coif 
		private static DbItemTemplate NecromancerEpicGloves = null; //Subterranean Gloves 
		private static DbItemTemplate NecromancerEpicVest = null; //Subterranean Hauberk 
		private static DbItemTemplate NecromancerEpicLegs = null; //Subterranean Legs 
		private static DbItemTemplate NecromancerEpicArms = null; //Subterranean Sleeves
		private static DbItemTemplate HereticEpicBoots = null;
		private static DbItemTemplate HereticEpicHelm = null;
		private static DbItemTemplate HereticEpicGloves = null;
		private static DbItemTemplate HereticEpicVest = null;
		private static DbItemTemplate HereticEpicLegs = null;
		private static DbItemTemplate HereticEpicArms = null;

		// Constructors
		public ShadowsLvl50EpicAlbQuest()
			: base()
		{
		}
		public ShadowsLvl50EpicAlbQuest(GamePlayer questingPlayer)
			: base(questingPlayer)
		{
		}

		public ShadowsLvl50EpicAlbQuest(GamePlayer questingPlayer, int step)
			: base(questingPlayer, step)
		{
		}

		public ShadowsLvl50EpicAlbQuest(GamePlayer questingPlayer, DbQuest dbQuest)
			: base(questingPlayer, dbQuest)
		{
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			

			#region NPC Declarations

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
				if (log.IsWarnEnabled)
					log.Warn("Could not find Lidmann Halsey, creating it ...");
				Lidmann = new GameNpc();
				Lidmann.Model = 64;
				Lidmann.Name = "Lidmann Halsey";
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
						Uragaig = npc as CailleachUragaig;
						break;
					}

			if (Uragaig == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Uragaig , creating it ...");
				Uragaig = new CailleachUragaig();
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

			#region Item Declarations

			#region misc
			sealed_pouch = GameServer.Database.FindObjectByKey<DbItemTemplate>("sealed_pouch");
			if (sealed_pouch == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sealed Pouch , creating it ...");
				sealed_pouch = new DbItemTemplate();
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
			#endregion
			// end item
			DbItemTemplate i = null;
			#region Mercenary
			MercenaryEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicBoots");
			if (MercenaryEpicBoots == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicBoots";
				i.Name = "Boots of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 722;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 9;
				i.Bonus4Type = (int)EStat.STR;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicBoots = i;

			}
			//end item
			// of the Shadowy Embers  Coif
			MercenaryEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicHelm");
			if (MercenaryEpicHelm == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicHelm";
				i.Name = "Coif of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicHelm = i;

			}
			//end item
			// of the Shadowy Embers  Gloves
			MercenaryEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicGloves");
			if (MercenaryEpicGloves == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicGloves";
				i.Name = "Gauntlets of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 721;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Crush;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicGloves = i;

			}
			// of the Shadowy Embers  Hauberk
			MercenaryEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicVest");
			if (MercenaryEpicVest == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicVest";
				i.Name = "Haurberk of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 718;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 48;
				i.Bonus2Type = (int)EProperty.MaxHealth;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicVest = i;

			}
			// of the Shadowy Embers  Legs
			MercenaryEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicLegs");
			if (MercenaryEpicLegs == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicLegs";
				i.Name = "Chausses of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 719;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicLegs = i;

			}
			// of the Shadowy Embers  Sleeves
			MercenaryEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("MercenaryEpicArms");
			if (MercenaryEpicArms == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "MercenaryEpicArms";
				i.Name = "Sleeves of the Shadowy Embers";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 720;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 12;
				i.Bonus4Type = (int)EStat.QUI;
				{
					GameServer.Database.AddObject(i);
				}

				MercenaryEpicArms = i;
			}
			#endregion
			#region Reaver
			//Reaver Epic Sleeves End
			ReaverEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicBoots");
			if (ReaverEpicBoots == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicBoots";
				i.Name = "Boots of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 23;
				i.Model = 1270;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 14;
				i.Bonus1Type = (int)EProperty.MaxMana;

				i.Bonus2 = 9;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				//                    i.Bonus4 = 10;
				//                    i.Bonus4Type = (int)eResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicBoots = i;

			}
			//end item
			//of Murky Secrets Coif
			ReaverEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicHelm");
			if (ReaverEpicHelm == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicHelm";
				i.Name = "Coif of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 21;
				i.Model = 1290; //NEED TO WORK ON..
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.PIE;

				i.Bonus2 = 6;
				i.Bonus2Type = (int)EProperty.Skill_Flexible_Weapon;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicHelm = i;

			}
			//end item
			//of Murky Secrets Gloves
			ReaverEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicGloves");
			if (ReaverEpicGloves == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicGloves";
				i.Name = "Gauntlets of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 22;
				i.Model = 1271;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Matter;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Crush;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicGloves = i;

			}
			//of Murky Secrets Hauberk
			ReaverEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicVest");
			if (ReaverEpicVest == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicVest";
				i.Name = "Hauberk of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 1267;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 48;
				i.Bonus1Type = (int)EProperty.MaxHealth;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.PIE;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicVest = i;

			}
			//of Murky Secrets Legs
			ReaverEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicLegs");
			if (ReaverEpicLegs == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicLegs";
				i.Name = "Chausses of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 27;
				i.Model = 1268;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.STR;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicLegs = i;

			}
			//of Murky Secrets Sleeves
			ReaverEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("ReaverEpicArms");
			if (ReaverEpicArms == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "ReaverEpicArms";
				i.Name = "Sleeves of Murky Secrets";
				i.Level = 50;
				i.Item_Type = 28;
				i.Model = 1269;
				i.IsDropable = true;
				i.IsPickable = true;
				i.DPS_AF = 100;
				i.SPD_ABS = 27;
				i.Object_Type = 35;
				i.Quality = 100;
				i.Weight = 22;
				i.Bonus = 35;
				i.MaxCondition = 50000;
				i.MaxDurability = 50000;
				i.Condition = 50000;
				i.Durability = 50000;


				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 4;
				i.Bonus4Type = (int)EProperty.Skill_Slashing;
				{
					GameServer.Database.AddObject(i);
				}

				ReaverEpicArms = i;
			}
			#endregion
			#region Infiltrator
			InfiltratorEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicBoots");
			if (InfiltratorEpicBoots == null)
			{
				InfiltratorEpicBoots = new DbItemTemplate();
				InfiltratorEpicBoots.Id_nb = "InfiltratorEpicBoots";
				InfiltratorEpicBoots.Name = "Shadow-Woven Boots";
				InfiltratorEpicBoots.Level = 50;
				InfiltratorEpicBoots.Item_Type = 23;
				InfiltratorEpicBoots.Model = 796;
				InfiltratorEpicBoots.IsDropable = true;
				InfiltratorEpicBoots.IsPickable = true;
				InfiltratorEpicBoots.DPS_AF = 100;
				InfiltratorEpicBoots.SPD_ABS = 10;
				InfiltratorEpicBoots.Object_Type = 33;
				InfiltratorEpicBoots.Quality = 100;
				InfiltratorEpicBoots.Weight = 22;
				InfiltratorEpicBoots.Bonus = 35;
				InfiltratorEpicBoots.MaxCondition = 50000;
				InfiltratorEpicBoots.MaxDurability = 50000;
				InfiltratorEpicBoots.Condition = 50000;
				InfiltratorEpicBoots.Durability = 50000;

				InfiltratorEpicBoots.Bonus1 = 13;
				InfiltratorEpicBoots.Bonus1Type = (int)EStat.QUI;

				InfiltratorEpicBoots.Bonus2 = 13;
				InfiltratorEpicBoots.Bonus2Type = (int)EStat.DEX;

				InfiltratorEpicBoots.Bonus3 = 8;
				InfiltratorEpicBoots.Bonus3Type = (int)EResist.Cold;

				InfiltratorEpicBoots.Bonus4 = 13;
				InfiltratorEpicBoots.Bonus4Type = (int)EStat.CON;
				{
					GameServer.Database.AddObject(InfiltratorEpicBoots);
				}

			}
			//end item
			//Shadow-Woven Coif
			InfiltratorEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicHelm");
			if (InfiltratorEpicHelm == null)
			{
				InfiltratorEpicHelm = new DbItemTemplate();
				InfiltratorEpicHelm.Id_nb = "InfiltratorEpicHelm";
				InfiltratorEpicHelm.Name = "Shadow-Woven Coif";
				InfiltratorEpicHelm.Level = 50;
				InfiltratorEpicHelm.Item_Type = 21;
				InfiltratorEpicHelm.Model = 1290; //NEED TO WORK ON..
				InfiltratorEpicHelm.IsDropable = true;
				InfiltratorEpicHelm.IsPickable = true;
				InfiltratorEpicHelm.DPS_AF = 100;
				InfiltratorEpicHelm.SPD_ABS = 10;
				InfiltratorEpicHelm.Object_Type = 33;
				InfiltratorEpicHelm.Quality = 100;
				InfiltratorEpicHelm.Weight = 22;
				InfiltratorEpicHelm.Bonus = 35;
				InfiltratorEpicHelm.MaxCondition = 50000;
				InfiltratorEpicHelm.MaxDurability = 50000;
				InfiltratorEpicHelm.Condition = 50000;
				InfiltratorEpicHelm.Durability = 50000;

				InfiltratorEpicHelm.Bonus1 = 13;
				InfiltratorEpicHelm.Bonus1Type = (int)EStat.DEX;

				InfiltratorEpicHelm.Bonus2 = 13;
				InfiltratorEpicHelm.Bonus2Type = (int)EStat.QUI;

				InfiltratorEpicHelm.Bonus3 = 8;
				InfiltratorEpicHelm.Bonus3Type = (int)EResist.Spirit;

				InfiltratorEpicHelm.Bonus4 = 13;
				InfiltratorEpicHelm.Bonus4Type = (int)EStat.STR;
				{
					GameServer.Database.AddObject(InfiltratorEpicHelm);
				}

			}
			//end item
			//Shadow-Woven Gloves
			InfiltratorEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicGloves");
			if (InfiltratorEpicGloves == null)
			{
				InfiltratorEpicGloves = new DbItemTemplate();
				InfiltratorEpicGloves.Id_nb = "InfiltratorEpicGloves";
				InfiltratorEpicGloves.Name = "Shadow-Woven Gloves";
				InfiltratorEpicGloves.Level = 50;
				InfiltratorEpicGloves.Item_Type = 22;
				InfiltratorEpicGloves.Model = 795;
				InfiltratorEpicGloves.IsDropable = true;
				InfiltratorEpicGloves.IsPickable = true;
				InfiltratorEpicGloves.DPS_AF = 100;
				InfiltratorEpicGloves.SPD_ABS = 10;
				InfiltratorEpicGloves.Object_Type = 33;
				InfiltratorEpicGloves.Quality = 100;
				InfiltratorEpicGloves.Weight = 22;
				InfiltratorEpicGloves.Bonus = 35;
				InfiltratorEpicGloves.MaxCondition = 50000;
				InfiltratorEpicGloves.MaxDurability = 50000;
				InfiltratorEpicGloves.Condition = 50000;
				InfiltratorEpicGloves.Durability = 50000;


				InfiltratorEpicGloves.Bonus1 = 18;
				InfiltratorEpicGloves.Bonus1Type = (int)EStat.STR;

				InfiltratorEpicGloves.Bonus2 = 21;
				InfiltratorEpicGloves.Bonus2Type = (int)EProperty.MaxHealth;

				InfiltratorEpicGloves.Bonus3 = 3;
				InfiltratorEpicGloves.Bonus3Type = (int)EProperty.Skill_Envenom;

				InfiltratorEpicGloves.Bonus4 = 3;
				InfiltratorEpicGloves.Bonus4Type = (int)EProperty.Skill_Critical_Strike;
				{
					GameServer.Database.AddObject(InfiltratorEpicGloves);
				}

			}
			//Shadow-Woven Hauberk
			InfiltratorEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicVest");
			if (InfiltratorEpicVest == null)
			{
				InfiltratorEpicVest = new DbItemTemplate();
				InfiltratorEpicVest.Id_nb = "InfiltratorEpicVest";
				InfiltratorEpicVest.Name = "Shadow-Woven Jerkin";
				InfiltratorEpicVest.Level = 50;
				InfiltratorEpicVest.Item_Type = 25;
				InfiltratorEpicVest.Model = 792;
				InfiltratorEpicVest.IsDropable = true;
				InfiltratorEpicVest.IsPickable = true;
				InfiltratorEpicVest.DPS_AF = 100;
				InfiltratorEpicVest.SPD_ABS = 10;
				InfiltratorEpicVest.Object_Type = 33;
				InfiltratorEpicVest.Quality = 100;
				InfiltratorEpicVest.Weight = 22;
				InfiltratorEpicVest.Bonus = 35;
				InfiltratorEpicVest.MaxCondition = 50000;
				InfiltratorEpicVest.MaxDurability = 50000;
				InfiltratorEpicVest.Condition = 50000;
				InfiltratorEpicVest.Durability = 50000;

				InfiltratorEpicVest.Bonus1 = 36;
				InfiltratorEpicVest.Bonus1Type = (int)EProperty.MaxHealth;

				InfiltratorEpicVest.Bonus2 = 16;
				InfiltratorEpicVest.Bonus2Type = (int)EStat.DEX;

				InfiltratorEpicVest.Bonus3 = 8;
				InfiltratorEpicVest.Bonus3Type = (int)EResist.Cold;

				InfiltratorEpicVest.Bonus4 = 8;
				InfiltratorEpicVest.Bonus4Type = (int)EResist.Body;
				{
					GameServer.Database.AddObject(InfiltratorEpicVest);
				}

			}
			//Shadow-Woven Legs
			InfiltratorEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicLegs");
			if (InfiltratorEpicLegs == null)
			{
				InfiltratorEpicLegs = new DbItemTemplate();
				InfiltratorEpicLegs.Id_nb = "InfiltratorEpicLegs";
				InfiltratorEpicLegs.Name = "Shadow-Woven Leggings";
				InfiltratorEpicLegs.Level = 50;
				InfiltratorEpicLegs.Item_Type = 27;
				InfiltratorEpicLegs.Model = 793;
				InfiltratorEpicLegs.IsDropable = true;
				InfiltratorEpicLegs.IsPickable = true;
				InfiltratorEpicLegs.DPS_AF = 100;
				InfiltratorEpicLegs.SPD_ABS = 10;
				InfiltratorEpicLegs.Object_Type = 33;
				InfiltratorEpicLegs.Quality = 100;
				InfiltratorEpicLegs.Weight = 22;
				InfiltratorEpicLegs.Bonus = 35;
				InfiltratorEpicLegs.MaxCondition = 50000;
				InfiltratorEpicLegs.MaxDurability = 50000;
				InfiltratorEpicLegs.Condition = 50000;
				InfiltratorEpicLegs.Durability = 50000;

				InfiltratorEpicLegs.Bonus1 = 21;
				InfiltratorEpicLegs.Bonus1Type = (int)EStat.CON;

				InfiltratorEpicLegs.Bonus2 = 16;
				InfiltratorEpicLegs.Bonus2Type = (int)EStat.QUI;

				InfiltratorEpicLegs.Bonus3 = 6;
				InfiltratorEpicLegs.Bonus3Type = (int)EResist.Heat;

				InfiltratorEpicLegs.Bonus4 = 6;
				InfiltratorEpicLegs.Bonus4Type = (int)EResist.Crush;
				{
					GameServer.Database.AddObject(InfiltratorEpicLegs);
				}

			}
			//Shadow-Woven Sleeves
			InfiltratorEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("InfiltratorEpicArms");
			if (InfiltratorEpicArms == null)
			{
				InfiltratorEpicArms = new DbItemTemplate();
				InfiltratorEpicArms.Id_nb = "InfiltratorEpicArms";
				InfiltratorEpicArms.Name = "Shadow-Woven Sleeves";
				InfiltratorEpicArms.Level = 50;
				InfiltratorEpicArms.Item_Type = 28;
				InfiltratorEpicArms.Model = 794;
				InfiltratorEpicArms.IsDropable = true;
				InfiltratorEpicArms.IsPickable = true;
				InfiltratorEpicArms.DPS_AF = 100;
				InfiltratorEpicArms.SPD_ABS = 10;
				InfiltratorEpicArms.Object_Type = 33;
				InfiltratorEpicArms.Quality = 100;
				InfiltratorEpicArms.Weight = 22;
				InfiltratorEpicArms.Bonus = 35;
				InfiltratorEpicArms.MaxCondition = 50000;
				InfiltratorEpicArms.MaxDurability = 50000;
				InfiltratorEpicArms.Condition = 50000;
				InfiltratorEpicArms.Durability = 50000;

				InfiltratorEpicArms.Bonus1 = 21;
				InfiltratorEpicArms.Bonus1Type = (int)EStat.DEX;

				InfiltratorEpicArms.Bonus2 = 18;
				InfiltratorEpicArms.Bonus2Type = (int)EStat.STR;

				InfiltratorEpicArms.Bonus3 = 6;
				InfiltratorEpicArms.Bonus3Type = (int)EResist.Matter;

				InfiltratorEpicArms.Bonus4 = 4;
				InfiltratorEpicArms.Bonus4Type = (int)EResist.Slash;
				{
					GameServer.Database.AddObject(InfiltratorEpicArms);
				}

			}
			#endregion
			#region Cabalist
			CabalistEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicBoots");
			if (CabalistEpicBoots == null)
			{
				CabalistEpicBoots = new DbItemTemplate();
				CabalistEpicBoots.Id_nb = "CabalistEpicBoots";
				CabalistEpicBoots.Name = "Warm Boots of the Construct";
				CabalistEpicBoots.Level = 50;
				CabalistEpicBoots.Item_Type = 23;
				CabalistEpicBoots.Model = 143;
				CabalistEpicBoots.IsDropable = true;
				CabalistEpicBoots.IsPickable = true;
				CabalistEpicBoots.DPS_AF = 50;
				CabalistEpicBoots.SPD_ABS = 0;
				CabalistEpicBoots.Object_Type = 32;
				CabalistEpicBoots.Quality = 100;
				CabalistEpicBoots.Weight = 22;
				CabalistEpicBoots.Bonus = 35;
				CabalistEpicBoots.MaxCondition = 50000;
				CabalistEpicBoots.MaxDurability = 50000;
				CabalistEpicBoots.Condition = 50000;
				CabalistEpicBoots.Durability = 50000;

				CabalistEpicBoots.Bonus1 = 22;
				CabalistEpicBoots.Bonus1Type = (int)EStat.DEX;

				CabalistEpicBoots.Bonus2 = 3;
				CabalistEpicBoots.Bonus2Type = (int)EProperty.Skill_Matter;

				CabalistEpicBoots.Bonus3 = 8;
				CabalistEpicBoots.Bonus3Type = (int)EResist.Slash;

				CabalistEpicBoots.Bonus4 = 8;
				CabalistEpicBoots.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(CabalistEpicBoots);
				}

			}
			//end item
			//Warm of the Construct Coif
			CabalistEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicHelm");
			if (CabalistEpicHelm == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "CabalistEpicHelm";
				i.Name = "Warm Coif of the Construct";
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

				i.Bonus1 = 21;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 13;
				i.Bonus2Type = (int)EStat.DEX;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}
				CabalistEpicHelm = i;

			}
			//end item
			//Warm of the Construct Gloves
			CabalistEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicGloves");
			if (CabalistEpicGloves == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "CabalistEpicGloves";
				i.Name = "Warm Gloves of the Construct";
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

				i.Bonus1 = 10;
				i.Bonus1Type = (int)EStat.DEX;

				i.Bonus2 = 10;
				i.Bonus2Type = (int)EStat.INT;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EProperty.MaxMana;

				i.Bonus4 = 10;
				i.Bonus4Type = (int)EResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}

				CabalistEpicGloves = i;

			}
			//Warm of the Construct Hauberk
			CabalistEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicVest");
			if (CabalistEpicVest == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "CabalistEpicVest";
				i.Name = "Warm Robe of the Construct";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 682;
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

				i.Bonus1 = 24;
				i.Bonus1Type = (int)EProperty.MaxHealth;

				i.Bonus2 = 14;
				i.Bonus2Type = (int)EProperty.MaxMana;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Crush;

				//                    i.Bonus4 = 10;
				//                    i.Bonus4Type = (int)eResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}

				CabalistEpicVest = i;

			}
			//Warm of the Construct Legs
			CabalistEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicLegs");
			if (CabalistEpicLegs == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "CabalistEpicLegs";
				i.Name = "Warm Leggings of the Construct";
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


				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Spirit;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}

				CabalistEpicLegs = i;

			}
			//Warm of the Construct Sleeves
			CabalistEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("CabalistEpicArms");
			if (CabalistEpicArms == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "CabalistEpicArms";
				i.Name = "Warm Sleeves of the Construct";
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


				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Body;

				i.Bonus3 = 16;
				i.Bonus3Type = (int)EStat.DEX;

				//                    i.Bonus4 = 10;
				//                    i.Bonus4Type = (int)eResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}
				CabalistEpicArms = i;

			}
			#endregion
			#region Necromancer
			NecromancerEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicBoots");
			if (NecromancerEpicBoots == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicBoots";
				i.Name = "Boots of Forbidden Rites";
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


				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Pain_working;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Slash;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Thrust;
				{
					GameServer.Database.AddObject(i);
				}

				NecromancerEpicBoots = i;
			}
			//end item
			//of Forbidden Rites Coif
			NecromancerEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicHelm");
			if (NecromancerEpicHelm == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicHelm";
				i.Name = "Cap of Forbidden Rites";
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


				i.Bonus1 = 21;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 13;
				i.Bonus2Type = (int)EStat.QUI;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Heat;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}
				NecromancerEpicHelm = i;

			}
			//end item
			//of Forbidden Rites Gloves
			NecromancerEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicGloves");
			if (NecromancerEpicGloves == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicGloves";
				i.Name = "Gloves of Forbidden Rites";
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


				i.Bonus1 = 10;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 10;
				i.Bonus2Type = (int)EStat.INT;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EProperty.MaxMana;

				i.Bonus4 = 10;
				i.Bonus4Type = (int)EResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}
				NecromancerEpicGloves = i;

			}
			//of Forbidden Rites Hauberk
			NecromancerEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicVest");
			if (NecromancerEpicVest == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicVest";
				i.Name = "Robe of Forbidden Rites";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 1266;
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


				i.Bonus1 = 24;
				i.Bonus1Type = (int)EProperty.MaxHealth;

				i.Bonus2 = 14;
				i.Bonus2Type = (int)EProperty.MaxMana;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Crush;

				//                    i.Bonus4 = 10;
				//                    i.Bonus4Type = (int)eResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}

				NecromancerEpicVest = i;

			}
			//of Forbidden Rites Legs
			NecromancerEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicLegs");
			if (NecromancerEpicLegs == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicLegs";
				i.Name = "Leggings of Forbidden Rites";
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


				i.Bonus1 = 22;
				i.Bonus1Type = (int)EStat.CON;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_Death_Servant;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}
				NecromancerEpicLegs = i;

			}
			//of Forbidden Rites Sleeves
			NecromancerEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("NecromancerEpicArms");
			if (NecromancerEpicArms == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "NecromancerEpicArms";
				i.Name = "Sleeves of Forbidden Rites";
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


				i.Bonus1 = 18;
				i.Bonus1Type = (int)EStat.INT;

				i.Bonus2 = 4;
				i.Bonus2Type = (int)EProperty.Skill_DeathSight;

				i.Bonus3 = 16;
				i.Bonus3Type = (int)EStat.DEX;

				//                    i.Bonus4 = 10;
				//                    i.Bonus4Type = (int)eResist.Energy;
				{
					GameServer.Database.AddObject(i);
				}
				NecromancerEpicArms = i;
				//Item Descriptions End
			}
			#endregion
			#region Heretic
			HereticEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicBoots");
			if (HereticEpicBoots == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicBoots";
				i.Name = "Boots of the Zealous Renegade";
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

				/*
				 *   Strength: 16 pts
				 *   Constitution: 18 pts
				 *   Slash Resist: 8%
				 *   Heat Resist: 8%
				 */

				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 18;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Slash;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Heat;
				{
					GameServer.Database.AddObject(i);
				}

				HereticEpicBoots = i;
			}
			//end item
			//of Forbidden Rites Coif
			HereticEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicHelm");
			if (HereticEpicHelm == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicHelm";
				i.Name = "Cap of the Zealous Renegade";
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

				/*
				 *   Piety: 15 pts
				 *   Thrust Resist: 6%
				 *   Cold Resist: 4%
				 *   Hits: 48 pts
				 */

				i.Bonus1 = 15;
				i.Bonus1Type = (int)EStat.PIE;

				i.Bonus2 = 6;
				i.Bonus2Type = (int)EResist.Thrust;

				i.Bonus3 = 4;
				i.Bonus3Type = (int)EResist.Cold;

				i.Bonus4 = 48;
				i.Bonus4Type = (int)EProperty.MaxHealth;
				{
					GameServer.Database.AddObject(i);
				}
				HereticEpicHelm = i;

			}
			//end item
			//of Forbidden Rites Gloves
			HereticEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicGloves");
			if (HereticEpicGloves == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicGloves";
				i.Name = "Gloves of the Zealous Renegade";
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

				/*
				 *   Strength: 9 pts
				 *   Power: 14 pts
				 *   Cold Resist: 8%
				 */
				i.Bonus1 = 9;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 14;
				i.Bonus2Type = (int)EProperty.MaxMana;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Cold;

				{
					GameServer.Database.AddObject(i);
				}
				HereticEpicGloves = i;

			}
			//of Forbidden Rites Hauberk
			HereticEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicVest");
			if (HereticEpicVest == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicVest";
				i.Name = "Robe of the Zealous Renegade";
				i.Level = 50;
				i.Item_Type = 25;
				i.Model = 2921;
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

				/*
				 *   Crush: +4 pts
				 *   Constitution: 16 pts
				 *   Dexterity: 15 pts
				 *   Cold Resist: 8%
				 */

				i.Bonus1 = 4;
				i.Bonus1Type = (int)EProperty.Skill_Crushing;

				i.Bonus2 = 16;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 15;
				i.Bonus3Type = (int)EStat.DEX;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Cold;
				{
					GameServer.Database.AddObject(i);
				}

				HereticEpicVest = i;

			}
			//of Forbidden Rites Legs
			HereticEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicLegs");
			if (HereticEpicLegs == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicLegs";
				i.Name = "Pants of the Zealous Renegade";
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

				/*
				 *   Strength: 19 pts
				 *   Constitution: 15 pts
				 *   Crush Resist: 8%
				 *   Matter Resist: 8%
				 */

				i.Bonus1 = 19;
				i.Bonus1Type = (int)EStat.STR;

				i.Bonus2 = 15;
				i.Bonus2Type = (int)EStat.CON;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Crush;

				i.Bonus4 = 8;
				i.Bonus4Type = (int)EResist.Matter;
				{
					GameServer.Database.AddObject(i);
				}
				HereticEpicLegs = i;

			}
			//of Forbidden Rites Sleeves
			HereticEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplate>("HereticEpicArms");
			if (HereticEpicArms == null)
			{
				i = new DbItemTemplate();
				i.Id_nb = "HereticEpicArms";
				i.Name = "Sleeves of the Zealous Renegade";
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

				/*
				 *   Piety: 16 pts
				 *   Thrust Resist: 8%
				 *   Body Resist: 8%
				 *   Flexible: 6 pts
				 */

				i.Bonus1 = 16;
				i.Bonus1Type = (int)EStat.PIE;

				i.Bonus2 = 8;
				i.Bonus2Type = (int)EResist.Thrust;

				i.Bonus3 = 8;
				i.Bonus3Type = (int)EResist.Body;

				i.Bonus4 = 6;
				i.Bonus4Type = (int)EProperty.Skill_Flexible_Weapon;
				{
					GameServer.Database.AddObject(i);
				}
				HereticEpicArms = i;
				//Item Descriptions End
			}
			#endregion

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Lidmann, GameObjectEvent.Interact, new CoreEventHandler(TalkToLidmann));
			GameEventMgr.AddHandler(Lidmann, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLidmann));

			/* Now we bring to Lidmann the possibility to give this quest to players */
			Lidmann.AddQuestToGive(typeof(ShadowsLvl50EpicAlbQuest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			//if not loaded, don't worry
			if (Lidmann == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Lidmann, GameObjectEvent.Interact, new CoreEventHandler(TalkToLidmann));
			GameEventMgr.RemoveHandler(Lidmann, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToLidmann));

			/* Now we remove to Lidmann the possibility to give this quest to players */
			Lidmann.RemoveQuestToGive(typeof(ShadowsLvl50EpicAlbQuest));
		}

		protected static void TalkToLidmann(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs)args).Source as GamePlayer;
			if (player == null)
				return;

			if (Lidmann.CanGiveQuest(typeof(ShadowsLvl50EpicAlbQuest), player) <= 0)
				return;

			//We also check if the player is already doing the quest
			ShadowsLvl50EpicAlbQuest quest = player.IsDoingQuest(typeof(ShadowsLvl50EpicAlbQuest)) as ShadowsLvl50EpicAlbQuest;

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
					Lidmann.SayTo(player, "Albion needs your [services]");
				}
			}

				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs)args;
				//Check player is already doing quest
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "services":
							player.Out.SendQuestSubscribeCommand(Lidmann, QuestMgr.GetIDForQuestType(typeof(ShadowsLvl50EpicAlbQuest)), "Will you help Lidmann [Defenders of Albion Level 50 Epic]?");
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
								if (player.Inventory.IsSlotsFree(6, EInventorySlot.FirstBackpack,
									    EInventorySlot.LastBackpack))
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
						if (player.Inventory.IsSlotsFree(6, EInventorySlot.FirstBackpack,
							    EInventorySlot.LastBackpack))
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
			if (player.IsDoingQuest(typeof(ShadowsLvl50EpicAlbQuest)) != null)
				return true;

			if (player.PlayerClass.ID != (byte)EPlayerClass.Reaver &&
				player.PlayerClass.ID != (byte)EPlayerClass.Mercenary &&
				player.PlayerClass.ID != (byte)EPlayerClass.Cabalist &&
				player.PlayerClass.ID != (byte)EPlayerClass.Necromancer &&
				player.PlayerClass.ID != (byte)EPlayerClass.Infiltrator &&
				player.PlayerClass.ID != (byte)EPlayerClass.Heretic)
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
			ShadowsLvl50EpicAlbQuest quest = player.IsDoingQuest(typeof(ShadowsLvl50EpicAlbQuest)) as ShadowsLvl50EpicAlbQuest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(ShadowsLvl50EpicAlbQuest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if (Lidmann.CanGiveQuest(typeof(ShadowsLvl50EpicAlbQuest), player) <= 0)
				return;

			if (player.IsDoingQuest(typeof(ShadowsLvl50EpicAlbQuest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				// Check to see if we can add quest
				if (!Lidmann.GiveQuest(typeof(ShadowsLvl50EpicAlbQuest), player, 1))
					return;

				player.Out.SendMessage("Kill Cailleach Uragaig in Lyonesse loc 29k, 33k!", EChatType.CT_System, EChatLoc.CL_PopupWindow);
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "Feast of the Decadent (Level 50 Guild of Shadows Epic)"; }
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

			if (player == null || player.IsDoingQuest(typeof(ShadowsLvl50EpicAlbQuest)) == null)
				return;

			if (sender != m_questPlayer)
				return;

			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs)args;
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
					if (player.Inventory.IsSlotsFree(6, EInventorySlot.FirstBackpack,
						    EInventorySlot.LastBackpack))
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

			switch ((EPlayerClass)m_questPlayer.PlayerClass.ID)
			{
				case EPlayerClass.Reaver:
					{
						GiveItem(m_questPlayer, ReaverEpicArms);
						GiveItem(m_questPlayer, ReaverEpicBoots);
						GiveItem(m_questPlayer, ReaverEpicGloves);
						GiveItem(m_questPlayer, ReaverEpicHelm);
						GiveItem(m_questPlayer, ReaverEpicLegs);
						GiveItem(m_questPlayer, ReaverEpicVest);
						break;
					}
				case EPlayerClass.Mercenary:
					{
						GiveItem(m_questPlayer, MercenaryEpicArms);
						GiveItem(m_questPlayer, MercenaryEpicBoots);
						GiveItem(m_questPlayer, MercenaryEpicGloves);
						GiveItem(m_questPlayer, MercenaryEpicHelm);
						GiveItem(m_questPlayer, MercenaryEpicLegs);
						GiveItem(m_questPlayer, MercenaryEpicVest);
						break;
					}
				case EPlayerClass.Cabalist:
					{
						GiveItem(m_questPlayer, CabalistEpicArms);
						GiveItem(m_questPlayer, CabalistEpicBoots);
						GiveItem(m_questPlayer, CabalistEpicGloves);
						GiveItem(m_questPlayer, CabalistEpicHelm);
						GiveItem(m_questPlayer, CabalistEpicLegs);
						GiveItem(m_questPlayer, CabalistEpicVest);
						break;
					}
				case EPlayerClass.Infiltrator:
					{
						GiveItem(m_questPlayer, InfiltratorEpicArms);
						GiveItem(m_questPlayer, InfiltratorEpicBoots);
						GiveItem(m_questPlayer, InfiltratorEpicGloves);
						GiveItem(m_questPlayer, InfiltratorEpicHelm);
						GiveItem(m_questPlayer, InfiltratorEpicLegs);
						GiveItem(m_questPlayer, InfiltratorEpicVest);
						break;
					}
				case EPlayerClass.Necromancer:
					{
						GiveItem(m_questPlayer, NecromancerEpicArms);
						GiveItem(m_questPlayer, NecromancerEpicBoots);
						GiveItem(m_questPlayer, NecromancerEpicGloves);
						GiveItem(m_questPlayer, NecromancerEpicHelm);
						GiveItem(m_questPlayer, NecromancerEpicLegs);
						GiveItem(m_questPlayer, NecromancerEpicVest);
						break;
					}
				case EPlayerClass.Heretic:
					{
						GiveItem(m_questPlayer, HereticEpicArms);
						GiveItem(m_questPlayer, HereticEpicBoots);
						GiveItem(m_questPlayer, HereticEpicGloves);
						GiveItem(m_questPlayer, HereticEpicHelm);
						GiveItem(m_questPlayer, HereticEpicLegs);
						GiveItem(m_questPlayer, HereticEpicVest);
						break;
					}
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
            * of the Shadowy Embers  Boots 
            * of the Shadowy Embers  Coif
            * of the Shadowy Embers  Gloves
            * of the Shadowy Embers  Hauberk
            * of the Shadowy Embers  Legs
            * of the Shadowy Embers  Sleeves
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
