using System;
using System.Reflection;
using DOL.Database;
using DOL.Events;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Midgard
{
	public class Mystic50Quest : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "Saving the Clan";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;

		private static GameNpc Danica = null; // Start NPC
		private static EpicKelic Kelic = null; // Mob to kill

		private static DbItemTemplates kelics_totem = null;
		private static DbItemTemplates SpiritmasterEpicBoots = null;
		private static DbItemTemplates SpiritmasterEpicHelm = null;
		private static DbItemTemplates SpiritmasterEpicGloves = null;
		private static DbItemTemplates SpiritmasterEpicLegs = null;
		private static DbItemTemplates SpiritmasterEpicArms = null;
		private static DbItemTemplates SpiritmasterEpicVest = null;
		private static DbItemTemplates RunemasterEpicBoots = null;
		private static DbItemTemplates RunemasterEpicHelm = null;
		private static DbItemTemplates RunemasterEpicGloves = null;
		private static DbItemTemplates RunemasterEpicLegs = null;
		private static DbItemTemplates RunemasterEpicArms = null;
		private static DbItemTemplates RunemasterEpicVest = null;
		private static DbItemTemplates BonedancerEpicBoots = null;
		private static DbItemTemplates BonedancerEpicHelm = null;
		private static DbItemTemplates BonedancerEpicGloves = null;
		private static DbItemTemplates BonedancerEpicLegs = null;
		private static DbItemTemplates BonedancerEpicArms = null;
		private static DbItemTemplates BonedancerEpicVest = null;
		private static DbItemTemplates WarlockEpicBoots = null;
		private static DbItemTemplates WarlockEpicHelm = null;
		private static DbItemTemplates WarlockEpicGloves = null;
		private static DbItemTemplates WarlockEpicLegs = null;
		private static DbItemTemplates WarlockEpicArms = null;
		private static DbItemTemplates WarlockEpicVest = null;

		// Constructors
		public Mystic50Quest() : base()
		{
		}

		public Mystic50Quest(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public Mystic50Quest(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public Mystic50Quest(GamePlayer questingPlayer, DbQuests dbQuest) : base(questingPlayer, dbQuest)
		{
		}


		[ScriptLoadedEvent]
		public static void ScriptLoaded(CoreEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.ServerProperties.LOAD_QUESTS)
				return;
			

			#region defineNPCs

			GameNpc[] npcs = WorldMgr.GetNPCsByName("Danica", ERealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 802818 && npc.Y == 727413)
					{
						Danica = npc;
						break;
					}

			if (Danica == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Danica , creating it ...");
				Danica = new GameNpc();
				Danica.Model = 227;
				Danica.Name = "Danica";
				Danica.GuildName = "";
				Danica.Realm = ERealm.Midgard;
				Danica.CurrentRegionID = 100;
				Danica.LoadEquipmentTemplateFromDatabase("Danica");
				Danica.Size = 51;
				Danica.Level = 50;
				Danica.X = 803559;
				Danica.Y = 723329;
				Danica.Z = 4719;
				Danica.Heading = 2193;
				Danica.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Danica.SaveIntoDatabase();
				}
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Kelic", ERealm.None);

			if (npcs.Length > 0)
				foreach (GameNpc npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 621577 && npc.Y == 745848)
					{
						Kelic = npc as EpicKelic;
						break;
					}

			if (Kelic == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Kelic , creating it ...");
				Kelic = new EpicKelic();
				Kelic.Model = 26;
				Kelic.Name = "Kelic";
				Kelic.GuildName = "";
				Kelic.Realm = ERealm.None;
				Kelic.CurrentRegionID = 100;
				Kelic.Size = 100;
				Kelic.Level = 65;
				Kelic.X = 621577;
				Kelic.Y = 745848;
				Kelic.Z = 4593;
				Kelic.Heading = 3538;
				Kelic.Flags ^= GameNpc.eFlags.GHOST;
				Kelic.MaxSpeedBase = 200;
				Kelic.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Kelic.SaveIntoDatabase();
				}
			}
			// end npc

				#endregion

			#region defineItems

				kelics_totem = GameServer.Database.FindObjectByKey<DbItemTemplates>("kelics_totem");
			if (kelics_totem == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Kelic's Totem , creating it ...");
				kelics_totem = new DbItemTemplates();
				kelics_totem.Id_nb = "kelics_totem";
				kelics_totem.Name = "Kelic's Totem";
				kelics_totem.Level = 8;
				kelics_totem.Item_Type = 0;
				kelics_totem.Model = 104;
				kelics_totem.IsDropable = false;
				kelics_totem.IsPickable = false;
				kelics_totem.DPS_AF = 0;
				kelics_totem.SPD_ABS = 0;
				kelics_totem.Object_Type = 0;
				kelics_totem.Hand = 0;
				kelics_totem.Type_Damage = 0;
				kelics_totem.Quality = 100;
				kelics_totem.Weight = 12;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(kelics_totem);
				}

			}

			SpiritmasterEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicBoots");
			if (SpiritmasterEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Boots , creating it ...");
				SpiritmasterEpicBoots = new DbItemTemplates();
				SpiritmasterEpicBoots.Id_nb = "SpiritmasterEpicBoots";
				SpiritmasterEpicBoots.Name = "Spirit Touched Boots";
				SpiritmasterEpicBoots.Level = 50;
				SpiritmasterEpicBoots.Item_Type = 23;
				SpiritmasterEpicBoots.Model = 803;
				SpiritmasterEpicBoots.IsDropable = true;
				SpiritmasterEpicBoots.IsPickable = true;
				SpiritmasterEpicBoots.DPS_AF = 50;
				SpiritmasterEpicBoots.SPD_ABS = 0;
				SpiritmasterEpicBoots.Object_Type = 32;
				SpiritmasterEpicBoots.Quality = 100;
				SpiritmasterEpicBoots.Weight = 22;
				SpiritmasterEpicBoots.Bonus = 35;
				SpiritmasterEpicBoots.MaxCondition = 50000;
				SpiritmasterEpicBoots.MaxDurability = 50000;
				SpiritmasterEpicBoots.Condition = 50000;
				SpiritmasterEpicBoots.Durability = 50000;

				SpiritmasterEpicBoots.Bonus1 = 16;
				SpiritmasterEpicBoots.Bonus1Type = (int) EStat.CON;

				SpiritmasterEpicBoots.Bonus2 = 16;
				SpiritmasterEpicBoots.Bonus2Type = (int) EStat.DEX;

				SpiritmasterEpicBoots.Bonus3 = 8;
				SpiritmasterEpicBoots.Bonus3Type = (int) EResist.Matter;

				SpiritmasterEpicBoots.Bonus4 = 10;
				SpiritmasterEpicBoots.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicBoots);
				}

			}
//end item
			SpiritmasterEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicHelm");
			if (SpiritmasterEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Helm , creating it ...");
				SpiritmasterEpicHelm = new DbItemTemplates();
				SpiritmasterEpicHelm.Id_nb = "SpiritmasterEpicHelm";
				SpiritmasterEpicHelm.Name = "Spirit Touched Cap";
				SpiritmasterEpicHelm.Level = 50;
				SpiritmasterEpicHelm.Item_Type = 21;
				SpiritmasterEpicHelm.Model = 825; //NEED TO WORK ON..
				SpiritmasterEpicHelm.IsDropable = true;
				SpiritmasterEpicHelm.IsPickable = true;
				SpiritmasterEpicHelm.DPS_AF = 50;
				SpiritmasterEpicHelm.SPD_ABS = 0;
				SpiritmasterEpicHelm.Object_Type = 32;
				SpiritmasterEpicHelm.Quality = 100;
				SpiritmasterEpicHelm.Weight = 22;
				SpiritmasterEpicHelm.Bonus = 35;
				SpiritmasterEpicHelm.MaxCondition = 50000;
				SpiritmasterEpicHelm.MaxDurability = 50000;
				SpiritmasterEpicHelm.Condition = 50000;
				SpiritmasterEpicHelm.Durability = 50000;

				SpiritmasterEpicHelm.Bonus1 = 4;
				SpiritmasterEpicHelm.Bonus1Type = (int) EProperty.Focus_Darkness;

				SpiritmasterEpicHelm.Bonus2 = 4;
				SpiritmasterEpicHelm.Bonus2Type = (int) EProperty.Focus_Suppression;

				SpiritmasterEpicHelm.Bonus3 = 13;
				SpiritmasterEpicHelm.Bonus3Type = (int) EStat.PIE;

				SpiritmasterEpicHelm.Bonus4 = 4;
				SpiritmasterEpicHelm.Bonus4Type = (int) EProperty.PowerRegenerationRate;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicHelm);
				}

			}
//end item
			SpiritmasterEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicGloves");
			if (SpiritmasterEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Gloves , creating it ...");
				SpiritmasterEpicGloves = new DbItemTemplates();
				SpiritmasterEpicGloves.Id_nb = "SpiritmasterEpicGloves";
				SpiritmasterEpicGloves.Name = "Spirit Touched Gloves ";
				SpiritmasterEpicGloves.Level = 50;
				SpiritmasterEpicGloves.Item_Type = 22;
				SpiritmasterEpicGloves.Model = 802;
				SpiritmasterEpicGloves.IsDropable = true;
				SpiritmasterEpicGloves.IsPickable = true;
				SpiritmasterEpicGloves.DPS_AF = 50;
				SpiritmasterEpicGloves.SPD_ABS = 0;
				SpiritmasterEpicGloves.Object_Type = 32;
				SpiritmasterEpicGloves.Quality = 100;
				SpiritmasterEpicGloves.Weight = 22;
				SpiritmasterEpicGloves.Bonus = 35;
				SpiritmasterEpicGloves.MaxCondition = 50000;
				SpiritmasterEpicGloves.MaxDurability = 50000;
				SpiritmasterEpicGloves.Condition = 50000;
				SpiritmasterEpicGloves.Durability = 50000;

				SpiritmasterEpicGloves.Bonus1 = 4;
				SpiritmasterEpicGloves.Bonus1Type = (int) EProperty.Focus_Summoning;

				SpiritmasterEpicGloves.Bonus2 = 13;
				SpiritmasterEpicGloves.Bonus2Type = (int) EStat.DEX;

				SpiritmasterEpicGloves.Bonus3 = 12;
				SpiritmasterEpicGloves.Bonus3Type = (int) EStat.PIE;

				SpiritmasterEpicGloves.Bonus4 = 4;
				SpiritmasterEpicGloves.Bonus4Type = (int) EProperty.PowerRegenerationRate;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicGloves);
				}

			}

			SpiritmasterEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicVest");
			if (SpiritmasterEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Vest , creating it ...");
				SpiritmasterEpicVest = new DbItemTemplates();
				SpiritmasterEpicVest.Id_nb = "SpiritmasterEpicVest";
				SpiritmasterEpicVest.Name = "Spirit Touched Vest";
				SpiritmasterEpicVest.Level = 50;
				SpiritmasterEpicVest.Item_Type = 25;
				SpiritmasterEpicVest.Model = 799;
				SpiritmasterEpicVest.IsDropable = true;
				SpiritmasterEpicVest.IsPickable = true;
				SpiritmasterEpicVest.DPS_AF = 50;
				SpiritmasterEpicVest.SPD_ABS = 0;
				SpiritmasterEpicVest.Object_Type = 32;
				SpiritmasterEpicVest.Quality = 100;
				SpiritmasterEpicVest.Weight = 22;
				SpiritmasterEpicVest.Bonus = 35;
				SpiritmasterEpicVest.MaxCondition = 50000;
				SpiritmasterEpicVest.MaxDurability = 50000;
				SpiritmasterEpicVest.Condition = 50000;
				SpiritmasterEpicVest.Durability = 50000;

				SpiritmasterEpicVest.Bonus1 = 12;
				SpiritmasterEpicVest.Bonus1Type = (int) EStat.DEX;

				SpiritmasterEpicVest.Bonus2 = 13;
				SpiritmasterEpicVest.Bonus2Type = (int) EStat.PIE;

				SpiritmasterEpicVest.Bonus3 = 12;
				SpiritmasterEpicVest.Bonus3Type = (int) EResist.Slash;

				SpiritmasterEpicVest.Bonus4 = 24;
				SpiritmasterEpicVest.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicVest);
				}

			}

			SpiritmasterEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicLegs");
			if (SpiritmasterEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Legs , creating it ...");
				SpiritmasterEpicLegs = new DbItemTemplates();
				SpiritmasterEpicLegs.Id_nb = "SpiritmasterEpicLegs";
				SpiritmasterEpicLegs.Name = "Spirit Touched Pants";
				SpiritmasterEpicLegs.Level = 50;
				SpiritmasterEpicLegs.Item_Type = 27;
				SpiritmasterEpicLegs.Model = 800;
				SpiritmasterEpicLegs.IsDropable = true;
				SpiritmasterEpicLegs.IsPickable = true;
				SpiritmasterEpicLegs.DPS_AF = 50;
				SpiritmasterEpicLegs.SPD_ABS = 0;
				SpiritmasterEpicLegs.Object_Type = 32;
				SpiritmasterEpicLegs.Quality = 100;
				SpiritmasterEpicLegs.Weight = 22;
				SpiritmasterEpicLegs.Bonus = 35;
				SpiritmasterEpicLegs.MaxCondition = 50000;
				SpiritmasterEpicLegs.MaxDurability = 50000;
				SpiritmasterEpicLegs.Condition = 50000;
				SpiritmasterEpicLegs.Durability = 50000;

				SpiritmasterEpicLegs.Bonus1 = 13;
				SpiritmasterEpicLegs.Bonus1Type = (int) EStat.CON;

				SpiritmasterEpicLegs.Bonus2 = 13;
				SpiritmasterEpicLegs.Bonus2Type = (int) EStat.DEX;

				SpiritmasterEpicLegs.Bonus3 = 12;
				SpiritmasterEpicLegs.Bonus3Type = (int) EResist.Crush;

				SpiritmasterEpicLegs.Bonus4 = 24;
				SpiritmasterEpicLegs.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicLegs);
				}

			}

			SpiritmasterEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("SpiritmasterEpicArms");
			if (SpiritmasterEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Spiritmaster Epic Arms , creating it ...");
				SpiritmasterEpicArms = new DbItemTemplates();
				SpiritmasterEpicArms.Id_nb = "SpiritmasterEpicArms";
				SpiritmasterEpicArms.Name = "Spirit Touched Sleeves";
				SpiritmasterEpicArms.Level = 50;
				SpiritmasterEpicArms.Item_Type = 28;
				SpiritmasterEpicArms.Model = 801;
				SpiritmasterEpicArms.IsDropable = true;
				SpiritmasterEpicArms.IsPickable = true;
				SpiritmasterEpicArms.DPS_AF = 50;
				SpiritmasterEpicArms.SPD_ABS = 0;
				SpiritmasterEpicArms.Object_Type = 32;
				SpiritmasterEpicArms.Quality = 100;
				SpiritmasterEpicArms.Weight = 22;
				SpiritmasterEpicArms.Bonus = 35;
				SpiritmasterEpicArms.MaxCondition = 50000;
				SpiritmasterEpicArms.MaxDurability = 50000;
				SpiritmasterEpicArms.Condition = 50000;
				SpiritmasterEpicArms.Durability = 50000;

				SpiritmasterEpicArms.Bonus1 = 9;
				SpiritmasterEpicArms.Bonus1Type = (int) EStat.PIE;

				SpiritmasterEpicArms.Bonus2 = 6;
				SpiritmasterEpicArms.Bonus2Type = (int) EResist.Thrust;

				SpiritmasterEpicArms.Bonus3 = 12;
				SpiritmasterEpicArms.Bonus3Type = (int) EProperty.MaxHealth;

				SpiritmasterEpicArms.Bonus4 = 8;
				SpiritmasterEpicArms.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(SpiritmasterEpicArms);
				}
			}

			RunemasterEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicBoots");
			if (RunemasterEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Boots , creating it ...");
				RunemasterEpicBoots = new DbItemTemplates();
				RunemasterEpicBoots.Id_nb = "RunemasterEpicBoots";
				RunemasterEpicBoots.Name = "Raven-Rune Boots";
				RunemasterEpicBoots.Level = 50;
				RunemasterEpicBoots.Item_Type = 23;
				RunemasterEpicBoots.Model = 707;
				RunemasterEpicBoots.IsDropable = true;
				RunemasterEpicBoots.IsPickable = true;
				RunemasterEpicBoots.DPS_AF = 50;
				RunemasterEpicBoots.SPD_ABS = 0;
				RunemasterEpicBoots.Object_Type = 32;
				RunemasterEpicBoots.Quality = 100;
				RunemasterEpicBoots.Weight = 22;
				RunemasterEpicBoots.Bonus = 35;
				RunemasterEpicBoots.MaxCondition = 50000;
				RunemasterEpicBoots.MaxDurability = 50000;
				RunemasterEpicBoots.Condition = 50000;
				RunemasterEpicBoots.Durability = 50000;

				RunemasterEpicBoots.Bonus1 = 16;
				RunemasterEpicBoots.Bonus1Type = (int) EStat.CON;

				RunemasterEpicBoots.Bonus2 = 16;
				RunemasterEpicBoots.Bonus2Type = (int) EStat.DEX;

				RunemasterEpicBoots.Bonus3 = 8;
				RunemasterEpicBoots.Bonus3Type = (int) EResist.Matter;

				RunemasterEpicBoots.Bonus4 = 10;
				RunemasterEpicBoots.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicBoots);
				}
			}
//end item
			RunemasterEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicHelm");
			if (RunemasterEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Helm , creating it ...");
				RunemasterEpicHelm = new DbItemTemplates();
				RunemasterEpicHelm.Id_nb = "RunemasterEpicHelm";
				RunemasterEpicHelm.Name = "Raven-Rune Cap";
				RunemasterEpicHelm.Level = 50;
				RunemasterEpicHelm.Item_Type = 21;
				RunemasterEpicHelm.Model = 825; //NEED TO WORK ON..
				RunemasterEpicHelm.IsDropable = true;
				RunemasterEpicHelm.IsPickable = true;
				RunemasterEpicHelm.DPS_AF = 50;
				RunemasterEpicHelm.SPD_ABS = 0;
				RunemasterEpicHelm.Object_Type = 32;
				RunemasterEpicHelm.Quality = 100;
				RunemasterEpicHelm.Weight = 22;
				RunemasterEpicHelm.Bonus = 35;
				RunemasterEpicHelm.MaxCondition = 50000;
				RunemasterEpicHelm.MaxDurability = 50000;
				RunemasterEpicHelm.Condition = 50000;
				RunemasterEpicHelm.Durability = 50000;

				RunemasterEpicHelm.Bonus1 = 4;
				RunemasterEpicHelm.Bonus1Type = (int) EProperty.Focus_Darkness;

				RunemasterEpicHelm.Bonus2 = 4;
				RunemasterEpicHelm.Bonus2Type = (int) EProperty.Focus_Suppression;

				RunemasterEpicHelm.Bonus3 = 13;
				RunemasterEpicHelm.Bonus3Type = (int) EStat.PIE;

				RunemasterEpicHelm.Bonus4 = 4;
				RunemasterEpicHelm.Bonus4Type = (int) EProperty.PowerRegenerationRate;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicHelm);
				}
			}
//end item
			RunemasterEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicGloves");
			if (RunemasterEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Gloves , creating it ...");
				RunemasterEpicGloves = new DbItemTemplates();
				RunemasterEpicGloves.Id_nb = "RunemasterEpicGloves";
				RunemasterEpicGloves.Name = "Raven-Rune Gloves ";
				RunemasterEpicGloves.Level = 50;
				RunemasterEpicGloves.Item_Type = 22;
				RunemasterEpicGloves.Model = 706;
				RunemasterEpicGloves.IsDropable = true;
				RunemasterEpicGloves.IsPickable = true;
				RunemasterEpicGloves.DPS_AF = 50;
				RunemasterEpicGloves.SPD_ABS = 0;
				RunemasterEpicGloves.Object_Type = 32;
				RunemasterEpicGloves.Quality = 100;
				RunemasterEpicGloves.Weight = 22;
				RunemasterEpicGloves.Bonus = 35;
				RunemasterEpicGloves.MaxCondition = 50000;
				RunemasterEpicGloves.MaxDurability = 50000;
				RunemasterEpicGloves.Condition = 50000;
				RunemasterEpicGloves.Durability = 50000;

				RunemasterEpicGloves.Bonus1 = 4;
				RunemasterEpicGloves.Bonus1Type = (int) EProperty.Focus_Summoning;

				RunemasterEpicGloves.Bonus2 = 13;
				RunemasterEpicGloves.Bonus2Type = (int) EStat.DEX;

				RunemasterEpicGloves.Bonus3 = 12;
				RunemasterEpicGloves.Bonus3Type = (int) EStat.PIE;

				RunemasterEpicGloves.Bonus4 = 6;
				RunemasterEpicGloves.Bonus4Type = (int) EProperty.PowerRegenerationRate;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicGloves);
				}
			}

			RunemasterEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicVest");
			if (RunemasterEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Vest , creating it ...");
				RunemasterEpicVest = new DbItemTemplates();
				RunemasterEpicVest.Id_nb = "RunemasterEpicVest";
				RunemasterEpicVest.Name = "Raven-Rune Vest";
				RunemasterEpicVest.Level = 50;
				RunemasterEpicVest.Item_Type = 25;
				RunemasterEpicVest.Model = 703;
				RunemasterEpicVest.IsDropable = true;
				RunemasterEpicVest.IsPickable = true;
				RunemasterEpicVest.DPS_AF = 50;
				RunemasterEpicVest.SPD_ABS = 0;
				RunemasterEpicVest.Object_Type = 32;
				RunemasterEpicVest.Quality = 100;
				RunemasterEpicVest.Weight = 22;
				RunemasterEpicVest.Bonus = 35;
				RunemasterEpicVest.MaxCondition = 50000;
				RunemasterEpicVest.MaxDurability = 50000;
				RunemasterEpicVest.Condition = 50000;
				RunemasterEpicVest.Durability = 50000;

				RunemasterEpicVest.Bonus1 = 12;
				RunemasterEpicVest.Bonus1Type = (int) EStat.DEX;

				RunemasterEpicVest.Bonus2 = 13;
				RunemasterEpicVest.Bonus2Type = (int) EStat.PIE;

				RunemasterEpicVest.Bonus3 = 12;
				RunemasterEpicVest.Bonus3Type = (int) EResist.Slash;

				RunemasterEpicVest.Bonus4 = 24;
				RunemasterEpicVest.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicVest);
				}
			}

			RunemasterEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicLegs");
			if (RunemasterEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Legs , creating it ...");
				RunemasterEpicLegs = new DbItemTemplates();
				RunemasterEpicLegs.Id_nb = "RunemasterEpicLegs";
				RunemasterEpicLegs.Name = "Raven-Rune Pants";
				RunemasterEpicLegs.Level = 50;
				RunemasterEpicLegs.Item_Type = 27;
				RunemasterEpicLegs.Model = 704;
				RunemasterEpicLegs.IsDropable = true;
				RunemasterEpicLegs.IsPickable = true;
				RunemasterEpicLegs.DPS_AF = 50;
				RunemasterEpicLegs.SPD_ABS = 0;
				RunemasterEpicLegs.Object_Type = 32;
				RunemasterEpicLegs.Quality = 100;
				RunemasterEpicLegs.Weight = 22;
				RunemasterEpicLegs.Bonus = 35;
				RunemasterEpicLegs.MaxCondition = 50000;
				RunemasterEpicLegs.MaxDurability = 50000;
				RunemasterEpicLegs.Condition = 50000;
				RunemasterEpicLegs.Durability = 50000;

				RunemasterEpicLegs.Bonus1 = 13;
				RunemasterEpicLegs.Bonus1Type = (int) EStat.CON;

				RunemasterEpicLegs.Bonus2 = 13;
				RunemasterEpicLegs.Bonus2Type = (int) EStat.DEX;

				RunemasterEpicLegs.Bonus3 = 12;
				RunemasterEpicLegs.Bonus3Type = (int) EResist.Crush;

				RunemasterEpicLegs.Bonus4 = 24;
				RunemasterEpicLegs.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicLegs);
				}
			}

			RunemasterEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("RunemasterEpicArms");
			if (RunemasterEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Runemaster Epic Arms , creating it ...");
				RunemasterEpicArms = new DbItemTemplates();
				RunemasterEpicArms.Id_nb = "RunemasterEpicArms";
				RunemasterEpicArms.Name = "Raven-Rune Sleeves";
				RunemasterEpicArms.Level = 50;
				RunemasterEpicArms.Item_Type = 28;
				RunemasterEpicArms.Model = 705;
				RunemasterEpicArms.IsDropable = true;
				RunemasterEpicArms.IsPickable = true;
				RunemasterEpicArms.DPS_AF = 50;
				RunemasterEpicArms.SPD_ABS = 0;
				RunemasterEpicArms.Object_Type = 32;
				RunemasterEpicArms.Quality = 100;
				RunemasterEpicArms.Weight = 22;
				RunemasterEpicArms.Bonus = 35;
				RunemasterEpicArms.MaxCondition = 50000;
				RunemasterEpicArms.MaxDurability = 50000;
				RunemasterEpicArms.Condition = 50000;
				RunemasterEpicArms.Durability = 50000;

				RunemasterEpicArms.Bonus1 = 9;
				RunemasterEpicArms.Bonus1Type = (int) EStat.PIE;

				RunemasterEpicArms.Bonus2 = 6;
				RunemasterEpicArms.Bonus2Type = (int) EResist.Thrust;

				RunemasterEpicArms.Bonus3 = 12;
				RunemasterEpicArms.Bonus3Type = (int) EProperty.MaxHealth;

				RunemasterEpicArms.Bonus4 = 8;
				RunemasterEpicArms.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(RunemasterEpicArms);
				}
			}

			BonedancerEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicBoots");
			if (BonedancerEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Boots , creating it ...");
				BonedancerEpicBoots = new DbItemTemplates();
				BonedancerEpicBoots.Id_nb = "BonedancerEpicBoots";
				BonedancerEpicBoots.Name = "Raven-Boned Boots";
				BonedancerEpicBoots.Level = 50;
				BonedancerEpicBoots.Item_Type = 23;
				BonedancerEpicBoots.Model = 1190;
				BonedancerEpicBoots.IsDropable = true;
				BonedancerEpicBoots.IsPickable = true;
				BonedancerEpicBoots.DPS_AF = 50;
				BonedancerEpicBoots.SPD_ABS = 0;
				BonedancerEpicBoots.Object_Type = 32;
				BonedancerEpicBoots.Quality = 100;
				BonedancerEpicBoots.Weight = 22;
				BonedancerEpicBoots.Bonus = 35;
				BonedancerEpicBoots.MaxCondition = 50000;
				BonedancerEpicBoots.MaxDurability = 50000;
				BonedancerEpicBoots.Condition = 50000;
				BonedancerEpicBoots.Durability = 50000;

				BonedancerEpicBoots.Bonus1 = 16;
				BonedancerEpicBoots.Bonus1Type = (int) EStat.CON;

				BonedancerEpicBoots.Bonus2 = 16;
				BonedancerEpicBoots.Bonus2Type = (int) EStat.DEX;

				BonedancerEpicBoots.Bonus3 = 8;
				BonedancerEpicBoots.Bonus3Type = (int) EResist.Matter;

				BonedancerEpicBoots.Bonus4 = 10;
				BonedancerEpicBoots.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicBoots);
				}

			}
//end item
			BonedancerEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicHelm");
			if (BonedancerEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Helm , creating it ...");
				BonedancerEpicHelm = new DbItemTemplates();
				BonedancerEpicHelm.Id_nb = "BonedancerEpicHelm";
				BonedancerEpicHelm.Name = "Raven-Boned Cap";
				BonedancerEpicHelm.Level = 50;
				BonedancerEpicHelm.Item_Type = 21;
				BonedancerEpicHelm.Model = 825; //NEED TO WORK ON..
				BonedancerEpicHelm.IsDropable = true;
				BonedancerEpicHelm.IsPickable = true;
				BonedancerEpicHelm.DPS_AF = 50;
				BonedancerEpicHelm.SPD_ABS = 0;
				BonedancerEpicHelm.Object_Type = 32;
				BonedancerEpicHelm.Quality = 100;
				BonedancerEpicHelm.Weight = 22;
				BonedancerEpicHelm.Bonus = 35;
				BonedancerEpicHelm.MaxCondition = 50000;
				BonedancerEpicHelm.MaxDurability = 50000;
				BonedancerEpicHelm.Condition = 50000;
				BonedancerEpicHelm.Durability = 50000;

				BonedancerEpicHelm.Bonus1 = 4;
				BonedancerEpicHelm.Bonus1Type = (int) EProperty.Focus_Suppression;

				BonedancerEpicHelm.Bonus2 = 13;
				BonedancerEpicHelm.Bonus2Type = (int) EStat.PIE;

				BonedancerEpicHelm.Bonus3 = 4;
				BonedancerEpicHelm.Bonus3Type = (int) EProperty.PowerRegenerationRate;

				BonedancerEpicHelm.Bonus4 = 4;
				BonedancerEpicHelm.Bonus4Type = (int) EProperty.Focus_BoneArmy;


				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicHelm);
				}

			}
//end item
			BonedancerEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicGloves");
			if (BonedancerEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Gloves , creating it ...");
				BonedancerEpicGloves = new DbItemTemplates();
				BonedancerEpicGloves.Id_nb = "BonedancerEpicGloves";
				BonedancerEpicGloves.Name = "Raven-Boned Gloves ";
				BonedancerEpicGloves.Level = 50;
				BonedancerEpicGloves.Item_Type = 22;
				BonedancerEpicGloves.Model = 1191;
				BonedancerEpicGloves.IsDropable = true;
				BonedancerEpicGloves.IsPickable = true;
				BonedancerEpicGloves.DPS_AF = 50;
				BonedancerEpicGloves.SPD_ABS = 0;
				BonedancerEpicGloves.Object_Type = 32;
				BonedancerEpicGloves.Quality = 100;
				BonedancerEpicGloves.Weight = 22;
				BonedancerEpicGloves.Bonus = 35;
				BonedancerEpicGloves.MaxCondition = 50000;
				BonedancerEpicGloves.MaxDurability = 50000;
				BonedancerEpicGloves.Condition = 50000;
				BonedancerEpicGloves.Durability = 50000;

				BonedancerEpicGloves.Bonus1 = 4;
				BonedancerEpicGloves.Bonus1Type = (int) EProperty.Focus_Darkness;

				BonedancerEpicGloves.Bonus2 = 13;
				BonedancerEpicGloves.Bonus2Type = (int) EStat.DEX;

				BonedancerEpicGloves.Bonus3 = 12;
				BonedancerEpicGloves.Bonus3Type = (int) EStat.PIE;

				BonedancerEpicGloves.Bonus4 = 6;
				BonedancerEpicGloves.Bonus4Type = (int) EProperty.PowerRegenerationRate;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicGloves);
				}
			}

			BonedancerEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicVest");
			if (BonedancerEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Vest , creating it ...");
				BonedancerEpicVest = new DbItemTemplates();
				BonedancerEpicVest.Id_nb = "BonedancerEpicVest";
				BonedancerEpicVest.Name = "Raven-Boned Vest";
				BonedancerEpicVest.Level = 50;
				BonedancerEpicVest.Item_Type = 25;
				BonedancerEpicVest.Model = 1187;
				BonedancerEpicVest.IsDropable = true;
				BonedancerEpicVest.IsPickable = true;
				BonedancerEpicVest.DPS_AF = 50;
				BonedancerEpicVest.SPD_ABS = 0;
				BonedancerEpicVest.Object_Type = 32;
				BonedancerEpicVest.Quality = 100;
				BonedancerEpicVest.Weight = 22;
				BonedancerEpicVest.Bonus = 35;
				BonedancerEpicVest.MaxCondition = 50000;
				BonedancerEpicVest.MaxDurability = 50000;
				BonedancerEpicVest.Condition = 50000;
				BonedancerEpicVest.Durability = 50000;

				BonedancerEpicVest.Bonus1 = 12;
				BonedancerEpicVest.Bonus1Type = (int) EStat.DEX;

				BonedancerEpicVest.Bonus2 = 13;
				BonedancerEpicVest.Bonus2Type = (int) EStat.PIE;

				BonedancerEpicVest.Bonus3 = 12;
				BonedancerEpicVest.Bonus3Type = (int) EResist.Slash;

				BonedancerEpicVest.Bonus4 = 24;
				BonedancerEpicVest.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicVest);
				}
			}

			BonedancerEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicLegs");
			if (BonedancerEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Legs , creating it ...");
				BonedancerEpicLegs = new DbItemTemplates();
				BonedancerEpicLegs.Id_nb = "BonedancerEpicLegs";
				BonedancerEpicLegs.Name = "Raven-Boned Pants";
				BonedancerEpicLegs.Level = 50;
				BonedancerEpicLegs.Item_Type = 27;
				BonedancerEpicLegs.Model = 1188;
				BonedancerEpicLegs.IsDropable = true;
				BonedancerEpicLegs.IsPickable = true;
				BonedancerEpicLegs.DPS_AF = 50;
				BonedancerEpicLegs.SPD_ABS = 0;
				BonedancerEpicLegs.Object_Type = 32;
				BonedancerEpicLegs.Quality = 100;
				BonedancerEpicLegs.Weight = 22;
				BonedancerEpicLegs.Bonus = 35;
				BonedancerEpicLegs.MaxCondition = 50000;
				BonedancerEpicLegs.MaxDurability = 50000;
				BonedancerEpicLegs.Condition = 50000;
				BonedancerEpicLegs.Durability = 50000;

				BonedancerEpicLegs.Bonus1 = 13;
				BonedancerEpicLegs.Bonus1Type = (int) EStat.CON;

				BonedancerEpicLegs.Bonus2 = 13;
				BonedancerEpicLegs.Bonus2Type = (int) EStat.DEX;

				BonedancerEpicLegs.Bonus3 = 12;
				BonedancerEpicLegs.Bonus3Type = (int) EResist.Crush;

				BonedancerEpicLegs.Bonus4 = 24;
				BonedancerEpicLegs.Bonus4Type = (int) EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicLegs);
				}

			}

			BonedancerEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("BonedancerEpicArms");
			if (BonedancerEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Bonedancer Epic Arms , creating it ...");
				BonedancerEpicArms = new DbItemTemplates();
				BonedancerEpicArms.Id_nb = "BonedancerEpicArms";
				BonedancerEpicArms.Name = "Raven-Boned Sleeves";
				BonedancerEpicArms.Level = 50;
				BonedancerEpicArms.Item_Type = 28;
				BonedancerEpicArms.Model = 1189;
				BonedancerEpicArms.IsDropable = true;
				BonedancerEpicArms.IsPickable = true;
				BonedancerEpicArms.DPS_AF = 50;
				BonedancerEpicArms.SPD_ABS = 0;
				BonedancerEpicArms.Object_Type = 32;
				BonedancerEpicArms.Quality = 100;
				BonedancerEpicArms.Weight = 22;
				BonedancerEpicArms.Bonus = 35;
				BonedancerEpicArms.MaxCondition = 50000;
				BonedancerEpicArms.MaxDurability = 50000;
				BonedancerEpicArms.Condition = 50000;
				BonedancerEpicArms.Durability = 50000;

				BonedancerEpicArms.Bonus1 = 9;
				BonedancerEpicArms.Bonus1Type = (int) EStat.PIE;

				BonedancerEpicArms.Bonus2 = 6;
				BonedancerEpicArms.Bonus2Type = (int) EResist.Thrust;

				BonedancerEpicArms.Bonus3 = 12;
				BonedancerEpicArms.Bonus3Type = (int) EProperty.MaxHealth;

				BonedancerEpicArms.Bonus4 = 8;
				BonedancerEpicArms.Bonus4Type = (int) EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(BonedancerEpicArms);
				}

			}
			#region Warlock
			WarlockEpicBoots = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicBoots");
			if (WarlockEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Boots , creating it ...");
				WarlockEpicBoots = new DbItemTemplates();
				WarlockEpicBoots.Id_nb = "WarlockEpicBoots";
				WarlockEpicBoots.Name = "Bewitched Soothsayer Boots";
				WarlockEpicBoots.Level = 50;
				WarlockEpicBoots.Item_Type = 23;
				WarlockEpicBoots.Model = 2937;
				WarlockEpicBoots.IsDropable = true;
				WarlockEpicBoots.IsPickable = true;
				WarlockEpicBoots.DPS_AF = 50;
				WarlockEpicBoots.SPD_ABS = 0;
				WarlockEpicBoots.Object_Type = 32;
				WarlockEpicBoots.Quality = 100;
				WarlockEpicBoots.Weight = 22;
				WarlockEpicBoots.Bonus = 35;
				WarlockEpicBoots.MaxCondition = 50000;
				WarlockEpicBoots.MaxDurability = 50000;
				WarlockEpicBoots.Condition = 50000;
				WarlockEpicBoots.Durability = 50000;

				/*
				 *   Constitution: 16 pts
				 *   Matter Resist: 8%
				 *   Hits: 48 pts
				 *   Heat Resist: 10%
				 */

				WarlockEpicBoots.Bonus1 = 16;
				WarlockEpicBoots.Bonus1Type = (int)EStat.CON;

				WarlockEpicBoots.Bonus2 = 8;
				WarlockEpicBoots.Bonus2Type = (int)EResist.Matter;

				WarlockEpicBoots.Bonus3 = 48;
				WarlockEpicBoots.Bonus3Type = (int)EProperty.MaxHealth;

				WarlockEpicBoots.Bonus4 = 10;
				WarlockEpicBoots.Bonus4Type = (int)EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicBoots);
				}

			}
			//end item
			WarlockEpicHelm = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicHelm");
			if (WarlockEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Helm , creating it ...");
				WarlockEpicHelm = new DbItemTemplates();
				WarlockEpicHelm.Id_nb = "WarlockEpicHelm";
				WarlockEpicHelm.Name = "Bewitched Soothsayer Cap";
				WarlockEpicHelm.Level = 50;
				WarlockEpicHelm.Item_Type = 21;
				WarlockEpicHelm.Model = 825; //NEED TO WORK ON..
				WarlockEpicHelm.IsDropable = true;
				WarlockEpicHelm.IsPickable = true;
				WarlockEpicHelm.DPS_AF = 50;
				WarlockEpicHelm.SPD_ABS = 0;
				WarlockEpicHelm.Object_Type = 32;
				WarlockEpicHelm.Quality = 100;
				WarlockEpicHelm.Weight = 22;
				WarlockEpicHelm.Bonus = 35;
				WarlockEpicHelm.MaxCondition = 50000;
				WarlockEpicHelm.MaxDurability = 50000;
				WarlockEpicHelm.Condition = 50000;
				WarlockEpicHelm.Durability = 50000;

				/*
				 *   Piety: 13 pts
				 *   Power: 4 pts
				 *   Cursing: +4 pts
				 *   Hexing: +4 pts
				 */

				WarlockEpicHelm.Bonus1 = 13;
				WarlockEpicHelm.Bonus1Type = (int)EStat.PIE;

				WarlockEpicHelm.Bonus2 = 4;
				WarlockEpicHelm.Bonus2Type = (int)EProperty.MaxMana;

				WarlockEpicHelm.Bonus3 = 4;
				WarlockEpicHelm.Bonus3Type = (int)EProperty.Skill_Cursing;

				WarlockEpicHelm.Bonus4 = 4;
				WarlockEpicHelm.Bonus4Type = (int)EProperty.Skill_Hexing;


				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicHelm);
				}

			}
			//end item
			WarlockEpicGloves = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicGloves");
			if (WarlockEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Gloves , creating it ...");
				WarlockEpicGloves = new DbItemTemplates();
				WarlockEpicGloves.Id_nb = "WarlockEpicGloves";
				WarlockEpicGloves.Name = "Bewitched Soothsayer Gloves ";
				WarlockEpicGloves.Level = 50;
				WarlockEpicGloves.Item_Type = 22;
				WarlockEpicGloves.Model = 2936;
				WarlockEpicGloves.IsDropable = true;
				WarlockEpicGloves.IsPickable = true;
				WarlockEpicGloves.DPS_AF = 50;
				WarlockEpicGloves.SPD_ABS = 0;
				WarlockEpicGloves.Object_Type = 32;
				WarlockEpicGloves.Quality = 100;
				WarlockEpicGloves.Weight = 22;
				WarlockEpicGloves.Bonus = 35;
				WarlockEpicGloves.MaxCondition = 50000;
				WarlockEpicGloves.MaxDurability = 50000;
				WarlockEpicGloves.Condition = 50000;
				WarlockEpicGloves.Durability = 50000;

				/*
				 *   Constitution: 13 pts
				 *   Piety: 12 pts
				 *   Power: 4 pts
				 *   Hexing: +4 pts
				 */

				WarlockEpicGloves.Bonus1 = 13;
				WarlockEpicGloves.Bonus1Type = (int)EStat.CON;

				WarlockEpicGloves.Bonus2 = 12;
				WarlockEpicGloves.Bonus2Type = (int)EStat.PIE;

				WarlockEpicGloves.Bonus3 = 4;
				WarlockEpicGloves.Bonus3Type = (int)EProperty.MaxMana;

				WarlockEpicGloves.Bonus4 = 4;
				WarlockEpicGloves.Bonus4Type = (int)EProperty.Skill_Hexing;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicGloves);
				}
			}

			WarlockEpicVest = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicVest");
			if (WarlockEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Vest , creating it ...");
				WarlockEpicVest = new DbItemTemplates();
				WarlockEpicVest.Id_nb = "WarlockEpicVest";
				WarlockEpicVest.Name = "Bewitched Soothsayer Vest";
				WarlockEpicVest.Level = 50;
				WarlockEpicVest.Item_Type = 25;
				WarlockEpicVest.Model = 2933;
				WarlockEpicVest.IsDropable = true;
				WarlockEpicVest.IsPickable = true;
				WarlockEpicVest.DPS_AF = 50;
				WarlockEpicVest.SPD_ABS = 0;
				WarlockEpicVest.Object_Type = 32;
				WarlockEpicVest.Quality = 100;
				WarlockEpicVest.Weight = 22;
				WarlockEpicVest.Bonus = 35;
				WarlockEpicVest.MaxCondition = 50000;
				WarlockEpicVest.MaxDurability = 50000;
				WarlockEpicVest.Condition = 50000;
				WarlockEpicVest.Durability = 50000;

				/*
				 *   Constitution: 12 pts
				 *   Piety: 13 pts
				 *   Slash Resist: 12%
				 *   Hits: 24 pts
				 */

				WarlockEpicVest.Bonus1 = 12;
				WarlockEpicVest.Bonus1Type = (int)EStat.CON;

				WarlockEpicVest.Bonus2 = 13;
				WarlockEpicVest.Bonus2Type = (int)EStat.PIE;

				WarlockEpicVest.Bonus3 = 12;
				WarlockEpicVest.Bonus3Type = (int)EResist.Slash;

				WarlockEpicVest.Bonus4 = 24;
				WarlockEpicVest.Bonus4Type = (int)EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicVest);
				}
			}

			WarlockEpicLegs = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicLegs");
			if (WarlockEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Legs , creating it ...");
				WarlockEpicLegs = new DbItemTemplates();
				WarlockEpicLegs.Id_nb = "WarlockEpicLegs";
				WarlockEpicLegs.Name = "Bewitched Soothsayer Pants";
				WarlockEpicLegs.Level = 50;
				WarlockEpicLegs.Item_Type = 27;
				WarlockEpicLegs.Model = 2934;
				WarlockEpicLegs.IsDropable = true;
				WarlockEpicLegs.IsPickable = true;
				WarlockEpicLegs.DPS_AF = 50;
				WarlockEpicLegs.SPD_ABS = 0;
				WarlockEpicLegs.Object_Type = 32;
				WarlockEpicLegs.Quality = 100;
				WarlockEpicLegs.Weight = 22;
				WarlockEpicLegs.Bonus = 35;
				WarlockEpicLegs.MaxCondition = 50000;
				WarlockEpicLegs.MaxDurability = 50000;
				WarlockEpicLegs.Condition = 50000;
				WarlockEpicLegs.Durability = 50000;

				/*
				 *   Constitution: 13 pts
				 *   Piety: 13 pts
				 *   Crush Resist: 12%
				 *   Hits: 24 pts
				 */

				WarlockEpicLegs.Bonus1 = 13;
				WarlockEpicLegs.Bonus1Type = (int)EStat.CON;

				WarlockEpicLegs.Bonus2 = 13;
				WarlockEpicLegs.Bonus2Type = (int)EStat.PIE;

				WarlockEpicLegs.Bonus3 = 12;
				WarlockEpicLegs.Bonus3Type = (int)EResist.Crush;

				WarlockEpicLegs.Bonus4 = 24;
				WarlockEpicLegs.Bonus4Type = (int)EProperty.MaxHealth;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicLegs);
				}

			}

			WarlockEpicArms = GameServer.Database.FindObjectByKey<DbItemTemplates>("WarlockEpicArms");
			if (WarlockEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Warlock Epic Arms , creating it ...");
				WarlockEpicArms = new DbItemTemplates();
				WarlockEpicArms.Id_nb = "WarlockEpicArms";
				WarlockEpicArms.Name = "Bewitched Soothsayer Sleeves";
				WarlockEpicArms.Level = 50;
				WarlockEpicArms.Item_Type = 28;
				WarlockEpicArms.Model = 1189;
				WarlockEpicArms.IsDropable = true;
				WarlockEpicArms.IsPickable = true;
				WarlockEpicArms.DPS_AF = 50;
				WarlockEpicArms.SPD_ABS = 0;
				WarlockEpicArms.Object_Type = 32;
				WarlockEpicArms.Quality = 100;
				WarlockEpicArms.Weight = 22;
				WarlockEpicArms.Bonus = 35;
				WarlockEpicArms.MaxCondition = 50000;
				WarlockEpicArms.MaxDurability = 50000;
				WarlockEpicArms.Condition = 50000;
				WarlockEpicArms.Durability = 50000;

				/*
				 *   Piety: 9 pts
				 *   Thrust Resist: 6%
				 *   Power: 12 pts
				 *   Heat Resist: 8%
				 */

				WarlockEpicArms.Bonus1 = 9;
				WarlockEpicArms.Bonus1Type = (int)EStat.PIE;

				WarlockEpicArms.Bonus2 = 6;
				WarlockEpicArms.Bonus2Type = (int)EResist.Thrust;

				WarlockEpicArms.Bonus3 = 12;
				WarlockEpicArms.Bonus3Type = (int)EProperty.MaxMana;

				WarlockEpicArms.Bonus4 = 8;
				WarlockEpicArms.Bonus4Type = (int)EResist.Heat;

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Database.AddObject(WarlockEpicArms);
				}

			}
			#endregion
			//Item Descriptions End

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Danica, GameObjectEvent.Interact, new CoreEventHandler(TalkToDanica));
			GameEventMgr.AddHandler(Danica, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToDanica));

			/* Now we bring to Danica the possibility to give this quest to players */
			Danica.AddQuestToGive(typeof (Mystic50Quest));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(CoreEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Danica == null || Kelic == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new CoreEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new CoreEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Danica, GameObjectEvent.Interact, new CoreEventHandler(TalkToDanica));
			GameEventMgr.RemoveHandler(Danica, GameLivingEvent.WhisperReceive, new CoreEventHandler(TalkToDanica));

			/* Now we remove to Danica the possibility to give this quest to players */
			Danica.RemoveQuestToGive(typeof (Mystic50Quest));
		}

		protected static void TalkToDanica(CoreEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Danica.CanGiveQuest(typeof (Mystic50Quest), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			Mystic50Quest quest = player.IsDoingQuest(typeof (Mystic50Quest)) as Mystic50Quest;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					switch (quest.Step)
					{
						case 1:
							Danica.SayTo(player, "Yes, you must face and defeat him! There is a note scrawled in the corner of the map that even in death Kelic is strong." +
								"He has gathered followers to protect him in his spirit state and they will come to his aid if he is attacked. Even though you have improved your skills quite a bit, " +
								"I would highley recommed taking some friends with you to face Kelic. It is imperative that you defeat him and obtain the totem he holds if I am to end the spell. " +
								"According to the map you can find Kelic in Raumarik. Head to the river in Raumarik and go north. When you reach the end of it, go northwest to the next river. " +
								"Cross the river and head west. Follow the snowline until you reach a group of trees. That is where you will find Kelic and his followers. " +
								"Return to me when you have the totem. May all the gods be with you.");
							break;
						case 2:
							Danica.SayTo(player, "It is good to see you were strong enough to survive Kelic. I can sense you have the controlling totem on you. Give me Kelic's [totem] now! Hurry!");
							break;
						case 3:
							Danica.SayTo(player, "The curse is broken and the clan is safe. They are in your debt, but I think Arnfinn, has come up with a suitable reward for you. There are six parts to it, so make sure you have room for them. Just let me know when you are ready, and then you can [take them] with our thanks!");
							break;
					}
				}
				else
				{
					Danica.SayTo(player, "Ah, this reveals exactly where Jango and his deserters took Kelic to dispose of him. He also has a note here about how strong Kelic really was. That [worries me].");
				}
			}
				// The player whispered to the NPC
			else if (e == GameLivingEvent.WhisperReceive)
			{
				WhisperReceiveEventArgs wArgs = (WhisperReceiveEventArgs) args;
				if (quest == null)
				{
					switch (wArgs.Text)
					{
						case "worries me":
							Danica.SayTo(player, "Yes, it worries me, but I think that you are ready to [face Kelic] and his minions.");
							break;
						case "face Kelic":
							player.Out.SendQuestSubscribeCommand(Danica, QuestMgr.GetIDForQuestType(typeof(Mystic50Quest)), "Will you face Kelic [Mystic Level 50 Epic]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "take them":
							if (quest.Step == 3)
							{
								if (player.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack,
									    eInventorySlot.LastBackpack))
								{
									Danica.SayTo(player, "You have earned this Epic Armor, wear it with honor!");
									quest.FinishQuest();
								}
								else
									player.Out.SendMessage("You do not have enough free space in your inventory!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
							}
							break;
						case "totem":
							if (quest.Step == 2)
							{
								RemoveItem(player, kelics_totem);
								quest.Step = 3;
								Danica.SayTo(player, "The curse is broken and the clan is safe. " +
								                     "They are in your debt, but I think Arnfinn, has come up with a suitable reward for you. " +
								                     "There are six parts to it, so make sure you have room for them. " +
								                     "Just let me know when you are ready, and then you can [take them] with our thanks!");
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
				ReceiveItemEventArgs rArgs = (ReceiveItemEventArgs) args;
				if (quest != null)
				{
					if (rArgs.Item.Id_nb == kelics_totem.Id_nb)
					{
						RemoveItem(player, kelics_totem);
						Danica.SayTo(player, "Ah, I can see how he wore the curse around the totem. I can now break the curse that is destroying the clan!");
						Danica.SayTo(player, "The curse is broken and the clan is safe. They are in your debt, but I think Arnfinn, has come up with a suitable reward for you. There are six parts to it, so make sure you have room for them. Just let me know when you are ready, and then you can [take them] with our thanks!");
						quest.Step = 3;
					}
				}
			}
		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (Mystic50Quest)) != null)
				return true;

			if (player.CharacterClass.ID != (byte) ECharacterClass.Spiritmaster &&
				player.CharacterClass.ID != (byte) ECharacterClass.Runemaster &&
				player.CharacterClass.ID != (byte) ECharacterClass.Bonedancer &&
				player.CharacterClass.ID != (byte) ECharacterClass.Warlock)
				return false;

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
			Mystic50Quest quest = player.IsDoingQuest(typeof (Mystic50Quest)) as Mystic50Quest;

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

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(Mystic50Quest)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Danica.CanGiveQuest(typeof (Mystic50Quest), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (Mystic50Quest)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", EChatType.CT_Say, EChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Danica.GiveQuest(typeof (Mystic50Quest), player, 1))
					return;

			}
			Danica.SayTo(player, "Yes, you must face and defeat him! There is a note scrawled in the corner of the map that even in death Kelic is strong. " +
			                     "He has gathered followers to protect him in his spirit state and they will come to his aid if he is attacked. Even though you have improved your skills quite a bit, " +
			                     "I would highley recommed taking some friends with you to face Kelic. It is imperative that you defeat him and obtain the totem he holds if I am to end the spell. " +
			                     "According to the map you can find Kelic in Raumarik. Head to the river in Raumarik and go north. When you reach the end of it, go northwest to the next river. " +
			                     "Cross the river and head west. Follow the snowline until you reach a group of trees. That is where you will find Kelic and his followers. " +
			                     "Return to me when you have the totem. May all the gods be with you.");
		}

		//Set quest name
		public override string Name
		{
			get { return "Saving the Clan (Level 50 Mystic Epic)"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "Find Kelic in Raumarik. Head to the river and go north. At the end go northwest to the next river, cross and head west. Follow the snowline until you reach a group of trees.";
					case 2:
						return "Return to Danica and give her the totem!";
					case 3:
						return "Speak with Danica for your reward!";
				}
				return base.Description;
			}
		}

		public override void Notify(CoreEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player==null || player.IsDoingQuest(typeof (Mystic50Quest)) == null)
				return;

			if (sender != m_questPlayer)
				return;
			
			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Name == Kelic.Name)
				{
					Step = 2;
					GiveItem(player, kelics_totem);
					m_questPlayer.Out.SendMessage("Kelic drops his Totem and you pick it up!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
				}
			}
			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Danica.Name && gArgs.Item.Id_nb == kelics_totem.Id_nb)
				{
					RemoveItem(Danica, player, kelics_totem);
					Danica.SayTo(player, "Ah, I can see how he wore the curse around the totem. I can now break the curse that is destroying the clan!");
					Danica.SayTo(player, "The curse is broken and the clan is safe. They are in your debt, but I think Arnfinn, has come up with a suitable reward for you. There are six parts to it, so make sure you have room for them. Just let me know when you are ready, and then you can [take them] with our thanks!");
					Step = 3;
				}
			}

		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			RemoveItem(m_questPlayer, kelics_totem, false);
		}

		public override void FinishQuest()
		{
			base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			switch ((ECharacterClass)m_questPlayer.CharacterClass.ID)
			{
				case ECharacterClass.Spiritmaster:
					{
						GiveItem(m_questPlayer, SpiritmasterEpicArms);
						GiveItem(m_questPlayer, SpiritmasterEpicBoots);
						GiveItem(m_questPlayer, SpiritmasterEpicGloves);
						GiveItem(m_questPlayer, SpiritmasterEpicHelm);
						GiveItem(m_questPlayer, SpiritmasterEpicLegs);
						GiveItem(m_questPlayer, SpiritmasterEpicVest);
						break;
					}
				case ECharacterClass.Runemaster:
					{
						GiveItem(m_questPlayer, RunemasterEpicArms);
						GiveItem(m_questPlayer, RunemasterEpicBoots);
						GiveItem(m_questPlayer, RunemasterEpicGloves);
						GiveItem(m_questPlayer, RunemasterEpicHelm);
						GiveItem(m_questPlayer, RunemasterEpicLegs);
						GiveItem(m_questPlayer, RunemasterEpicVest);
						break;
					}
				case ECharacterClass.Bonedancer:
					{
						GiveItem(m_questPlayer, BonedancerEpicArms);
						GiveItem(m_questPlayer, BonedancerEpicBoots);
						GiveItem(m_questPlayer, BonedancerEpicGloves);
						GiveItem(m_questPlayer, BonedancerEpicHelm);
						GiveItem(m_questPlayer, BonedancerEpicLegs);
						GiveItem(m_questPlayer, BonedancerEpicVest);
						break;
					}
				case ECharacterClass.Warlock:
					{
						GiveItem(m_questPlayer, WarlockEpicArms);
						GiveItem(m_questPlayer, WarlockEpicBoots);
						GiveItem(m_questPlayer, WarlockEpicGloves);
						GiveItem(m_questPlayer, WarlockEpicHelm);
						GiveItem(m_questPlayer, WarlockEpicLegs);
						GiveItem(m_questPlayer, WarlockEpicVest);
						break;
					}
			}
			Danica.SayTo(m_questPlayer, "May it serve you well, knowing that you have helped preserve the history of Midgard!");

			m_questPlayer.GainExperience(EXpSource.Quest, 1937768448, true);
			//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
				
		}

		#region Allakhazam Epic Source

		/*
        *#25 talk to Inaksha
        *#26 seek out Loken in Raumarik Loc 47k, 25k, 4k, and kill him purp and 2 blue adds 
        *#27 return to Inaksha 
        *#28 give her the ball of flame
        *#29 talk with Inaksha about Loken�s demise
        *#30 go to Miri in Jordheim 
        *#31 give her the sealed pouch
        *#32 you get your epic armor as a reward
        */

		/*
Spirit Touched Boots 
Spirit Touched Cap 
Spirit Touched Gloves 
Spirit Touched Pants 
Spirit Touched Sleeves 
Spirit Touched Vest 
Raven-Rune Boots 
Raven-Rune Cap 
Raven-Rune Gloves 
Raven-Rune Pants 
Raven-Rune Sleeves 
Raven-Rune Vest 
Raven-boned Boots 
Raven-Boned Cap 
Raven-boned Gloves 
Raven-Boned Pants 
Raven-Boned Sleeves 
Bone-rune Vest 
        */

		#endregion
	}
}
