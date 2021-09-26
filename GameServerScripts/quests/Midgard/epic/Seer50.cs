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
/*
*Author         : Etaew - Fallen Realms
*Source         : http://camelot.allakhazam.com
*Date           : 21 November 2004
*Quest Name     : The Desire of a God (Level 50)
*Quest Classes  : Healer, Shaman (Seer)
*Quest Version  : v1.2
*
*Changes:
*   The epic armour should now have the correct durability and condition
*   The armour will now be correctly rewarded with all peices
*   The items used in the quest cannot be traded or dropped
*   The items / itemtemplates / NPCs are created if they are not found
*   Add bonuses to epic items
*ToDo:
*   Add correct Text
*   Find Helm ModelID for epics..
*/

using System;
using System.Linq;
using System.Reflection;
using Atlas.DataLayer.Models;
using DOL.Events;
using DOL.GS;
using DOL.GS.PacketHandler;
using log4net;

namespace DOL.GS.Quests.Midgard
{
	public class Seer_50 : BaseQuest
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		protected const string questTitle = "The Desire of a God";
		protected const int minimumLevel = 50;
		protected const int maximumLevel = 50;

		private static GameNPC Inaksha = null; // Start NPC
		private static GameNPC Loken = null; // Mob to kill
		private static GameNPC Miri = null; // Trainer for reward

		private static ItemTemplate ball_of_flame = null; //ball of flame
		private static ItemTemplate sealed_pouch = null; //sealed pouch
		private static ItemTemplate HealerEpicBoots = null; //Valhalla Touched Boots 
		private static ItemTemplate HealerEpicHelm = null; //Valhalla Touched Coif 
		private static ItemTemplate HealerEpicGloves = null; //Valhalla Touched Gloves 
		private static ItemTemplate HealerEpicVest = null; //Valhalla Touched Hauberk 
		private static ItemTemplate HealerEpicLegs = null; //Valhalla Touched Legs 
		private static ItemTemplate HealerEpicArms = null; //Valhalla Touched Sleeves 
		private static ItemTemplate ShamanEpicBoots = null; //Subterranean Boots 
		private static ItemTemplate ShamanEpicHelm = null; //Subterranean Coif 
		private static ItemTemplate ShamanEpicGloves = null; //Subterranean Gloves 
		private static ItemTemplate ShamanEpicVest = null; //Subterranean Hauberk 
		private static ItemTemplate ShamanEpicLegs = null; //Subterranean Legs 
		private static ItemTemplate ShamanEpicArms = null; //Subterranean Sleeves         

		// Constructors
		public Seer_50() : base()
		{
		}

		public Seer_50(GamePlayer questingPlayer) : base(questingPlayer)
		{
		}

		public Seer_50(GamePlayer questingPlayer, int step) : base(questingPlayer, step)
		{
		}

		public Seer_50(GamePlayer questingPlayer, Quest dbQuest) : base(questingPlayer, dbQuest)
		{
		}

		[ScriptLoadedEvent]
		public static void ScriptLoaded(DOLEvent e, object sender, EventArgs args)
		{
			if (!ServerProperties.Properties.LOAD_QUESTS)
				return;
			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initializing ...");

			#region defineNPCs

			GameNPC[] npcs = WorldMgr.GetNPCsByName("Inaksha", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 805929 && npc.Y == 702449)
					{
						Inaksha = npc;
						break;
					}

			if (Inaksha == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Inaksha , creating it ...");
				Inaksha = new GameNPC();
				Inaksha.Model = 193;
				Inaksha.Name = "Inaksha";
				Inaksha.GuildName = "";
				Inaksha.Realm = eRealm.Midgard;
				Inaksha.CurrentRegionID = 100;
				Inaksha.Size = 50;
				Inaksha.Level = 41;
				Inaksha.X = 805929;
				Inaksha.Y = 702449;
				Inaksha.Z = 4960;
				Inaksha.Heading = 2116;
				Inaksha.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Inaksha.SaveIntoDatabase();
				}
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Loken", eRealm.None);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 100 && npc.X == 636784 && npc.Y == 762433)
					{
						Loken = npc;
						break;
					}

			if (Loken == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Loken , creating it ...");
				Loken = new GameNPC();
				Loken.Model = 212;
				Loken.Name = "Loken";
				Loken.GuildName = "";
				Loken.Realm = eRealm.None;
				Loken.CurrentRegionID = 100;
				Loken.Size = 50;
				Loken.Level = 65;
				Loken.X = 636784;
				Loken.Y = 762433;
				Loken.Z = 4596;
				Loken.Heading = 3777;
				Loken.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Loken.SaveIntoDatabase();
				}
			}
			// end npc

			npcs = WorldMgr.GetNPCsByName("Miri", eRealm.Midgard);

			if (npcs.Length > 0)
				foreach (GameNPC npc in npcs)
					if (npc.CurrentRegionID == 101 && npc.X == 30641 && npc.Y == 32093)
					{
						Miri = npc;
						break;
					}

			if (Miri == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Miri , creating it ...");
				Miri = new GameNPC();
				Miri.Model = 220;
				Miri.Name = "Miri";
				Miri.GuildName = "";
				Miri.Realm = eRealm.Midgard;
				Miri.CurrentRegionID = 101;
				Miri.Size = 50;
				Miri.Level = 43;
				Miri.X = 30641;
				Miri.Y = 32093;
				Miri.Z = 8305;
				Miri.Heading = 3037;
				Miri.AddToWorld();
				if (SAVE_INTO_DATABASE)
				{
					Miri.SaveIntoDatabase();
				}
			}
			// end npc

			#endregion

			#region defineItems

			ball_of_flame = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ball_of_flame");
			if (ball_of_flame == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find ball_of_flame , creating it ...");
				ball_of_flame = new ItemTemplate();
				ball_of_flame.KeyName = "ball_of_flame";
				ball_of_flame.Name = "Ball of Flame";
				ball_of_flame.Level = 8;
				ball_of_flame.ItemType = 29;
				ball_of_flame.Model = 601;
				ball_of_flame.IsDropable = false;
				ball_of_flame.IsPickable = false;
				ball_of_flame.DPS_AF = 0;
				ball_of_flame.SPD_ABS = 0;
				ball_of_flame.ObjectType = 41;
				ball_of_flame.Hand = 0;
				ball_of_flame.TypeDamage = 0;
				ball_of_flame.Quality = 100;
				ball_of_flame.Weight = 12;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ball_of_flame);
				}
			}

// end item
			sealed_pouch = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "sealed_pouch");
			if (sealed_pouch == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Sealed Pouch , creating it ...");
				sealed_pouch = new ItemTemplate();
				sealed_pouch.KeyName = "sealed_pouch";
				sealed_pouch.Name = "Sealed Pouch";
				sealed_pouch.Level = 8;
				sealed_pouch.ItemType = 29;
				sealed_pouch.Model = 488;
				sealed_pouch.IsDropable = false;
				sealed_pouch.IsPickable = false;
				sealed_pouch.DPS_AF = 0;
				sealed_pouch.SPD_ABS = 0;
				sealed_pouch.ObjectType = 41;
				sealed_pouch.Hand = 0;
				sealed_pouch.TypeDamage = 0;
				sealed_pouch.Quality = 100;
				sealed_pouch.Weight = 12;
				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(sealed_pouch);
				}
			}
// end item

			//Valhalla Touched Boots
			HealerEpicBoots = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicBoots");
			if (HealerEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healers Epic Boots , creating it ...");
				HealerEpicBoots = new ItemTemplate();
				HealerEpicBoots.KeyName = "HealerEpicBoots";
				HealerEpicBoots.Name = "Valhalla Touched Boots";
				HealerEpicBoots.Level = 50;
				HealerEpicBoots.ItemType = 23;
				HealerEpicBoots.Model = 702;
				HealerEpicBoots.IsDropable = true;
				HealerEpicBoots.IsPickable = true;
				HealerEpicBoots.DPS_AF = 100;
				HealerEpicBoots.SPD_ABS = 27;
				HealerEpicBoots.ObjectType = 35;
				HealerEpicBoots.Quality = 100;
				HealerEpicBoots.Weight = 22;
				HealerEpicBoots.ItemBonus = 35;
				HealerEpicBoots.MaxCondition = 50000;
				HealerEpicBoots.MaxDurability = 50000;
				HealerEpicBoots.Condition = 50000;
				HealerEpicBoots.Durability = 50000;

				HealerEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.CON, BonusValue = 12 });

				HealerEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.DEX, BonusValue = 12 });

				HealerEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eStat.QUI, BonusValue = 12 });

				HealerEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eProperty.MaxHealth, BonusValue = 21 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicBoots);
				}
			}
//end item
			//Valhalla Touched Coif 
			HealerEpicHelm = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicHelm");
			if (HealerEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healers Epic Helm , creating it ...");
				HealerEpicHelm = new ItemTemplate();
				HealerEpicHelm.KeyName = "HealerEpicHelm";
				HealerEpicHelm.Name = "Valhalla Touched Coif";
				HealerEpicHelm.Level = 50;
				HealerEpicHelm.ItemType = 21;
				HealerEpicHelm.Model = 1291; //NEED TO WORK ON..
				HealerEpicHelm.IsDropable = true;
				HealerEpicHelm.IsPickable = true;
				HealerEpicHelm.DPS_AF = 100;
				HealerEpicHelm.SPD_ABS = 27;
				HealerEpicHelm.ObjectType = 35;
				HealerEpicHelm.Quality = 100;
				HealerEpicHelm.Weight = 22;
				HealerEpicHelm.ItemBonus = 35;
				HealerEpicHelm.MaxCondition = 50000;
				HealerEpicHelm.MaxDurability = 50000;
				HealerEpicHelm.Condition = 50000;
				HealerEpicHelm.Durability = 50000;

				HealerEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Augmentation, BonusValue = 4 });

				HealerEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 18 });

				HealerEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Slash, BonusValue = 4 });

				HealerEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eProperty.PowerRegenerationRate, BonusValue = 6 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicHelm);
				}

			}
//end item
			//Valhalla Touched Gloves 
			HealerEpicGloves = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicGloves");
			if (HealerEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healers Epic Gloves , creating it ...");
				HealerEpicGloves = new ItemTemplate();
				HealerEpicGloves.KeyName = "HealerEpicGloves";
				HealerEpicGloves.Name = "Valhalla Touched Gloves ";
				HealerEpicGloves.Level = 50;
				HealerEpicGloves.ItemType = 22;
				HealerEpicGloves.Model = 701;
				HealerEpicGloves.IsDropable = true;
				HealerEpicGloves.IsPickable = true;
				HealerEpicGloves.DPS_AF = 100;
				HealerEpicGloves.SPD_ABS = 27;
				HealerEpicGloves.ObjectType = 35;
				HealerEpicGloves.Quality = 100;
				HealerEpicGloves.Weight = 22;
				HealerEpicGloves.ItemBonus = 35;
				HealerEpicGloves.MaxCondition = 50000;
				HealerEpicGloves.MaxDurability = 50000;
				HealerEpicGloves.Condition = 50000;
				HealerEpicGloves.Durability = 50000;

				HealerEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Mending, BonusValue = 4 });

				HealerEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 16 });

				HealerEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Crush, BonusValue = 4 });

				HealerEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eProperty.PowerRegenerationRate, BonusValue = 6 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicGloves);
				}

			}
			//Valhalla Touched Hauberk 
			HealerEpicVest = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicVest");
			if (HealerEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healers Epic Vest , creating it ...");
				HealerEpicVest = new ItemTemplate();
				HealerEpicVest.KeyName = "HealerEpicVest";
				HealerEpicVest.Name = "Valhalla Touched Haukberk";
				HealerEpicVest.Level = 50;
				HealerEpicVest.ItemType = 25;
				HealerEpicVest.Model = 698;
				HealerEpicVest.IsDropable = true;
				HealerEpicVest.IsPickable = true;
				HealerEpicVest.DPS_AF = 100;
				HealerEpicVest.SPD_ABS = 27;
				HealerEpicVest.ObjectType = 35;
				HealerEpicVest.Quality = 100;
				HealerEpicVest.Weight = 22;
				HealerEpicVest.ItemBonus = 35;
				HealerEpicVest.MaxCondition = 50000;
				HealerEpicVest.MaxDurability = 50000;
				HealerEpicVest.Condition = 50000;
				HealerEpicVest.Durability = 50000;

				HealerEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.CON, BonusValue = 16 });

				HealerEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 16 });

				HealerEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Cold, BonusValue = 8 });

				HealerEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Heat, BonusValue = 10 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicVest);
				}

			}
			//Valhalla Touched Legs 
			HealerEpicLegs = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicLegs");
			if (HealerEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healers Epic Legs , creating it ...");
				HealerEpicLegs = new ItemTemplate();
				HealerEpicLegs.KeyName = "HealerEpicLegs";
				HealerEpicLegs.Name = "Valhalla Touched Legs";
				HealerEpicLegs.Level = 50;
				HealerEpicLegs.ItemType = 27;
				HealerEpicLegs.Model = 699;
				HealerEpicLegs.IsDropable = true;
				HealerEpicLegs.IsPickable = true;
				HealerEpicLegs.DPS_AF = 100;
				HealerEpicLegs.SPD_ABS = 27;
				HealerEpicLegs.ObjectType = 35;
				HealerEpicLegs.Quality = 100;
				HealerEpicLegs.Weight = 22;
				HealerEpicLegs.ItemBonus = 35;
				HealerEpicLegs.MaxCondition = 50000;
				HealerEpicLegs.MaxDurability = 50000;
				HealerEpicLegs.Condition = 50000;
				HealerEpicLegs.Durability = 50000;

				HealerEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.STR, BonusValue = 15 });

				HealerEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.CON, BonusValue = 16 });

				HealerEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Spirit, BonusValue = 10 });

				HealerEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Energy, BonusValue = 10 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicLegs);
				}

			}
			//Valhalla Touched Sleeves 
			HealerEpicArms = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "HealerEpicArms");
			if (HealerEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Healer Epic Arms , creating it ...");
				HealerEpicArms = new ItemTemplate();
				HealerEpicArms.KeyName = "HealerEpicArms";
				HealerEpicArms.Name = "Valhalla Touched Sleeves";
				HealerEpicArms.Level = 50;
				HealerEpicArms.ItemType = 28;
				HealerEpicArms.Model = 700;
				HealerEpicArms.IsDropable = true;
				HealerEpicArms.IsPickable = true;
				HealerEpicArms.DPS_AF = 100;
				HealerEpicArms.SPD_ABS = 27;
				HealerEpicArms.ObjectType = 35;
				HealerEpicArms.Quality = 100;
				HealerEpicArms.Weight = 22;
				HealerEpicArms.ItemBonus = 35;
				HealerEpicArms.MaxCondition = 50000;
				HealerEpicArms.MaxDurability = 50000;
				HealerEpicArms.Condition = 50000;
				HealerEpicArms.Durability = 50000;

				HealerEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Mending, BonusValue = 4 });

				HealerEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.STR, BonusValue = 13 });

				HealerEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eStat.PIE, BonusValue = 15 });

				HealerEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Matter, BonusValue = 6 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(HealerEpicArms);
				}

			}
			//Subterranean Boots 
			ShamanEpicBoots = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicBoots");
			if (ShamanEpicBoots == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Boots , creating it ...");
				ShamanEpicBoots = new ItemTemplate();
				ShamanEpicBoots.KeyName = "ShamanEpicBoots";
				ShamanEpicBoots.Name = "Subterranean Boots";
				ShamanEpicBoots.Level = 50;
				ShamanEpicBoots.ItemType = 23;
				ShamanEpicBoots.Model = 770;
				ShamanEpicBoots.IsDropable = true;
				ShamanEpicBoots.IsPickable = true;
				ShamanEpicBoots.DPS_AF = 100;
				ShamanEpicBoots.SPD_ABS = 27;
				ShamanEpicBoots.ObjectType = 35;
				ShamanEpicBoots.Quality = 100;
				ShamanEpicBoots.Weight = 22;
				ShamanEpicBoots.ItemBonus = 35;
				ShamanEpicBoots.MaxCondition = 50000;
				ShamanEpicBoots.MaxDurability = 50000;
				ShamanEpicBoots.Condition = 50000;
				ShamanEpicBoots.Durability = 50000;

				ShamanEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.DEX, BonusValue = 13 });

				ShamanEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.QUI, BonusValue = 13 });

				ShamanEpicBoots.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eProperty.MaxHealth, BonusValue = 39 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicBoots);
				}

			}
			//Subterranean Coif 
			ShamanEpicHelm = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicHelm");
			if (ShamanEpicHelm == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Helm , creating it ...");
				ShamanEpicHelm = new ItemTemplate();
				ShamanEpicHelm.KeyName = "ShamanEpicHelm";
				ShamanEpicHelm.Name = "Subterranean Coif";
				ShamanEpicHelm.Level = 50;
				ShamanEpicHelm.ItemType = 21;
				ShamanEpicHelm.Model = 63; //NEED TO WORK ON..
				ShamanEpicHelm.IsDropable = true;
				ShamanEpicHelm.IsPickable = true;
				ShamanEpicHelm.DPS_AF = 100;
				ShamanEpicHelm.SPD_ABS = 27;
				ShamanEpicHelm.ObjectType = 35;
				ShamanEpicHelm.Quality = 100;
				ShamanEpicHelm.Weight = 22;
				ShamanEpicHelm.ItemBonus = 35;
				ShamanEpicHelm.MaxCondition = 50000;
				ShamanEpicHelm.MaxDurability = 50000;
				ShamanEpicHelm.Condition = 50000;
				ShamanEpicHelm.Durability = 50000;

				ShamanEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Mending, BonusValue = 4 });

				ShamanEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 18 });

				ShamanEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Thrust, BonusValue = 4 });

				ShamanEpicHelm.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eProperty.PowerRegenerationRate, BonusValue = 6 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicHelm);
				}

			}
			//Subterranean Gloves 
			ShamanEpicGloves = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicGloves");
			if (ShamanEpicGloves == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Gloves , creating it ...");
				ShamanEpicGloves = new ItemTemplate();
				ShamanEpicGloves.KeyName = "ShamanEpicGloves";
				ShamanEpicGloves.Name = "Subterranean Gloves";
				ShamanEpicGloves.Level = 50;
				ShamanEpicGloves.ItemType = 22;
				ShamanEpicGloves.Model = 769;
				ShamanEpicGloves.IsDropable = true;
				ShamanEpicGloves.IsPickable = true;
				ShamanEpicGloves.DPS_AF = 100;
				ShamanEpicGloves.SPD_ABS = 27;
				ShamanEpicGloves.ObjectType = 35;
				ShamanEpicGloves.Quality = 100;
				ShamanEpicGloves.Weight = 22;
				ShamanEpicGloves.ItemBonus = 35;
				ShamanEpicGloves.MaxCondition = 50000;
				ShamanEpicGloves.MaxDurability = 50000;
				ShamanEpicGloves.Condition = 50000;
				ShamanEpicGloves.Durability = 50000;

				ShamanEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Subterranean, BonusValue = 4 });

				ShamanEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 18 });

				ShamanEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Crush, BonusValue = 4 });

				ShamanEpicGloves.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eProperty.PowerRegenerationRate, BonusValue = 6 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicGloves);
				}

			}
			//Subterranean Hauberk 
			ShamanEpicVest = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicVest");
			if (ShamanEpicVest == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Vest , creating it ...");
				ShamanEpicVest = new ItemTemplate();
				ShamanEpicVest.KeyName = "ShamanEpicVest";
				ShamanEpicVest.Name = "Subterranean Hauberk";
				ShamanEpicVest.Level = 50;
				ShamanEpicVest.ItemType = 25;
				ShamanEpicVest.Model = 766;
				ShamanEpicVest.IsDropable = true;
				ShamanEpicVest.IsPickable = true;
				ShamanEpicVest.DPS_AF = 100;
				ShamanEpicVest.SPD_ABS = 27;
				ShamanEpicVest.ObjectType = 35;
				ShamanEpicVest.Quality = 100;
				ShamanEpicVest.Weight = 22;
				ShamanEpicVest.ItemBonus = 35;
				ShamanEpicVest.MaxCondition = 50000;
				ShamanEpicVest.MaxDurability = 50000;
				ShamanEpicVest.Condition = 50000;
				ShamanEpicVest.Durability = 50000;

				ShamanEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.CON, BonusValue = 16 });

				ShamanEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.PIE, BonusValue = 16 });

				ShamanEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Matter, BonusValue = 10 });

				ShamanEpicVest.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Heat, BonusValue = 8 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicVest);
				}

			}
			//Subterranean Legs 
			ShamanEpicLegs = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicLegs");
			if (ShamanEpicLegs == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Legs , creating it ...");
				ShamanEpicLegs = new ItemTemplate();
				ShamanEpicLegs.KeyName = "ShamanEpicLegs";
				ShamanEpicLegs.Name = "Subterranean Legs";
				ShamanEpicLegs.Level = 50;
				ShamanEpicLegs.ItemType = 27;
				ShamanEpicLegs.Model = 767;
				ShamanEpicLegs.IsDropable = true;
				ShamanEpicLegs.IsPickable = true;
				ShamanEpicLegs.DPS_AF = 100;
				ShamanEpicLegs.SPD_ABS = 27;
				ShamanEpicLegs.ObjectType = 35;
				ShamanEpicLegs.Quality = 100;
				ShamanEpicLegs.Weight = 22;
				ShamanEpicLegs.ItemBonus = 35;
				ShamanEpicLegs.MaxCondition = 50000;
				ShamanEpicLegs.MaxDurability = 50000;
				ShamanEpicLegs.Condition = 50000;
				ShamanEpicLegs.Durability = 50000;

				ShamanEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eStat.CON, BonusValue = 16 });

				ShamanEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.DEX, BonusValue = 16 });

				ShamanEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eResist.Cold, BonusValue = 8 });

				ShamanEpicLegs.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Spirit, BonusValue = 10 });

				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicLegs);
				}

			}
			//Subterranean Sleeves 
			ShamanEpicArms = GameServer.Database.ItemTemplates.FirstOrDefault(x => x.KeyName == "ShamanEpicArms");
			if (ShamanEpicArms == null)
			{
				if (log.IsWarnEnabled)
					log.Warn("Could not find Shaman Epic Arms , creating it ...");
				ShamanEpicArms = new ItemTemplate();
				ShamanEpicArms.KeyName = "ShamanEpicArms";
				ShamanEpicArms.Name = "Subterranean Sleeves";
				ShamanEpicArms.Level = 50;
				ShamanEpicArms.ItemType = 28;
				ShamanEpicArms.Model = 768;
				ShamanEpicArms.IsDropable = true;
				ShamanEpicArms.IsPickable = true;
				ShamanEpicArms.DPS_AF = 100;
				ShamanEpicArms.SPD_ABS = 27;
				ShamanEpicArms.ObjectType = 35;
				ShamanEpicArms.Quality = 100;
				ShamanEpicArms.Weight = 22;
				ShamanEpicArms.ItemBonus = 35;
				ShamanEpicArms.MaxCondition = 50000;
				ShamanEpicArms.MaxDurability = 50000;
				ShamanEpicArms.Condition = 50000;
				ShamanEpicArms.Durability = 50000;

				ShamanEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 1, BonusType = (int)eProperty.Skill_Augmentation, BonusValue = 4 });

				ShamanEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 2, BonusType = (int)eStat.STR, BonusValue = 12 });

				ShamanEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 3, BonusType = (int)eStat.PIE, BonusValue = 18 });

				ShamanEpicArms.Bonuses.Add(new ItemBonus() { BonusOrder = 4, BonusType = (int)eResist.Energy, BonusValue = 6 });


				if (SAVE_INTO_DATABASE)
				{
					GameServer.Instance.SaveDataObject(ShamanEpicArms);
				}

			}
//Shaman Epic Sleeves End
//Item Descriptions End

			#endregion

			GameEventMgr.AddHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.AddHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.AddHandler(Inaksha, GameObjectEvent.Interact, new DOLEventHandler(TalkToInaksha));
			GameEventMgr.AddHandler(Inaksha, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToInaksha));
			GameEventMgr.AddHandler(Miri, GameObjectEvent.Interact, new DOLEventHandler(TalkToMiri));
			GameEventMgr.AddHandler(Miri, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMiri));

			/* Now we bring to Inaksha the possibility to give this quest to players */
			Inaksha.AddQuestToGive(typeof (Seer_50));

			if (log.IsInfoEnabled)
				log.Info("Quest \"" + questTitle + "\" initialized");
		}

		[ScriptUnloadedEvent]
		public static void ScriptUnloaded(DOLEvent e, object sender, EventArgs args)
		{
			//if not loaded, don't worry
			if (Inaksha == null || Miri == null)
				return;
			// remove handlers
			GameEventMgr.RemoveHandler(GamePlayerEvent.AcceptQuest, new DOLEventHandler(SubscribeQuest));
			GameEventMgr.RemoveHandler(GamePlayerEvent.DeclineQuest, new DOLEventHandler(SubscribeQuest));

			GameEventMgr.RemoveHandler(Inaksha, GameObjectEvent.Interact, new DOLEventHandler(TalkToInaksha));
			GameEventMgr.RemoveHandler(Inaksha, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToInaksha));
			GameEventMgr.RemoveHandler(Miri, GameObjectEvent.Interact, new DOLEventHandler(TalkToMiri));
			GameEventMgr.RemoveHandler(Miri, GameLivingEvent.WhisperReceive, new DOLEventHandler(TalkToMiri));

			/* Now we remove to Inaksha the possibility to give this quest to players */
			Inaksha.RemoveQuestToGive(typeof (Seer_50));
		}

		protected static void TalkToInaksha(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Inaksha.CanGiveQuest(typeof (Seer_50), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			Seer_50 quest = player.IsDoingQuest(typeof (Seer_50)) as Seer_50;

			if (e == GameObjectEvent.Interact)
			{
				// Nag to finish quest
				if (quest != null)
				{
					Inaksha.SayTo(player, "Check your Journal for instructions!");
				}
				else
				{
					Inaksha.SayTo(player, "Midgard needs your [services]");
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
							player.Out.SendQuestSubscribeCommand(Inaksha, QuestMgr.GetIDForQuestType(typeof(Seer_50)), "Will you help Inaksha [Seer Level 50 Epic]?");
							break;
					}
				}
				else
				{
					switch (wArgs.Text)
					{
						case "dead":
							if (quest.Step == 3)
							{
								Inaksha.SayTo(player, "Take this sealed pouch to Miri in Jordheim for your reward!");
								GiveItem(Inaksha, player, sealed_pouch);
								quest.Step = 4;
							}
							break;
						case "abort":
							player.Out.SendCustomDialog("Do you really want to abort this quest, \nall items gained during quest will be lost?", new CustomDialogResponse(CheckPlayerAbortQuest));
							break;
					}
				}

			}

		}

		protected static void TalkToMiri(DOLEvent e, object sender, EventArgs args)
		{
			//We get the player from the event arguments and check if he qualifies		
			GamePlayer player = ((SourceEventArgs) args).Source as GamePlayer;
			if (player == null)
				return;

			if(Inaksha.CanGiveQuest(typeof (Seer_50), player)  <= 0)
				return;

			//We also check if the player is already doing the quest
			Seer_50 quest = player.IsDoingQuest(typeof (Seer_50)) as Seer_50;

			if (e == GameObjectEvent.Interact)
			{
				if (quest != null)
				{
					Miri.SayTo(player, "Check your journal for instructions!");
				}
				else
				{
					Miri.SayTo(player, "I need your help to seek out loken in raumarik Loc 47k, 25k, 4k, and kill him ");
				}
			}

		}

		public override bool CheckQuestQualification(GamePlayer player)
		{
			// if the player is already doing the quest his level is no longer of relevance
			if (player.IsDoingQuest(typeof (Seer_50)) != null)
				return true;

			if (player.CharacterClass.ID != (byte) eCharacterClass.Shaman &&
				player.CharacterClass.ID != (byte) eCharacterClass.Healer)
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
			Seer_50 quest = player.IsDoingQuest(typeof (Seer_50)) as Seer_50;

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

		protected static void SubscribeQuest(DOLEvent e, object sender, EventArgs args)
		{
			QuestEventArgs qargs = args as QuestEventArgs;
			if (qargs == null)
				return;

			if (qargs.QuestID != QuestMgr.GetIDForQuestType(typeof(Seer_50)))
				return;

			if (e == GamePlayerEvent.AcceptQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x01);
			else if (e == GamePlayerEvent.DeclineQuest)
				CheckPlayerAcceptQuest(qargs.Player, 0x00);
		}

		private static void CheckPlayerAcceptQuest(GamePlayer player, byte response)
		{
			if(Inaksha.CanGiveQuest(typeof (Seer_50), player)  <= 0)
				return;

			if (player.IsDoingQuest(typeof (Seer_50)) != null)
				return;

			if (response == 0x00)
			{
				player.Out.SendMessage("Our God forgives your laziness, just look out for stray lightning bolts.", eChatType.CT_Say, eChatLoc.CL_PopupWindow);
			}
			else
			{
				//Check if we can add the quest!
				if (!Inaksha.GiveQuest(typeof (Seer_50), player, 1))
					return;

				player.Out.SendMessage("Good now go kill him!", eChatType.CT_System, eChatLoc.CL_PopupWindow);
			}
		}

		//Set quest name
		public override string Name
		{
			get { return "The Desire of a God (Level 50 Seer Epic)"; }
		}

		// Define Steps
		public override string Description
		{
			get
			{
				switch (Step)
				{
					case 1:
						return "[Step #1] Seek out Loken in Raumarik Loc 47k, 25k kill him!";
					case 2:
						return "[Step #2] Return to Inaksha and give her the Ball of Flame!";
					case 3:
						return "[Step #3] Talk with Inaksha about Loken�s demise!";
					case 4:
						return "[Step #4] Go to Miri in Jordheim and give her the Sealed Pouch for your reward!";
				}
				return base.Description;
			}
		}

		public override void Notify(DOLEvent e, object sender, EventArgs args)
		{
			GamePlayer player = sender as GamePlayer;

			if (player == null || player.IsDoingQuest(typeof(Seer_50)) == null)
				return;

			if (Step == 1 && e == GameLivingEvent.EnemyKilled)
			{
				EnemyKilledEventArgs gArgs = (EnemyKilledEventArgs) args;
				if (gArgs.Target.Name == Loken.Name)
				{
					m_questPlayer.Out.SendMessage("You get a ball of flame", eChatType.CT_System, eChatLoc.CL_SystemWindow);
					GiveItem(m_questPlayer, ball_of_flame);
					Step = 2;
					return;
				}
			}

			if (Step == 2 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Inaksha.Name && gArgs.Item.ItemTemplate.KeyName == ball_of_flame.KeyName)
				{
					RemoveItem(Inaksha, player, ball_of_flame);
					Inaksha.SayTo(player, "So it seems Logan's [dead]");
					Step = 3;
					return;
				}
			}

			if (Step == 4 && e == GamePlayerEvent.GiveItem)
			{
				GiveItemEventArgs gArgs = (GiveItemEventArgs) args;
				if (gArgs.Target.Name == Miri.Name && gArgs.Item.ItemTemplate.KeyName == sealed_pouch.KeyName)
				{
					Miri.SayTo(player, "You have earned this Epic Armour!");
					FinishQuest();
					return;
				}
			}
		}

		public override void AbortQuest()
		{
			base.AbortQuest(); //Defined in Quest, changes the state, stores in DB etc ...

			RemoveItem(m_questPlayer, sealed_pouch, false);
			RemoveItem(m_questPlayer, ball_of_flame, false);
		}

		public override void FinishQuest()
		{
			if (m_questPlayer.Inventory.IsSlotsFree(6, eInventorySlot.FirstBackpack, eInventorySlot.LastBackpack))
			{
				RemoveItem(Miri, m_questPlayer, sealed_pouch);

				base.FinishQuest(); //Defined in Quest, changes the state, stores in DB etc ...

				if (m_questPlayer.CharacterClass.ID == (byte)eCharacterClass.Shaman)
				{
					GiveItem(m_questPlayer, ShamanEpicArms);
					GiveItem(m_questPlayer, ShamanEpicBoots);
					GiveItem(m_questPlayer, ShamanEpicGloves);
					GiveItem(m_questPlayer, ShamanEpicHelm);
					GiveItem(m_questPlayer, ShamanEpicLegs);
					GiveItem(m_questPlayer, ShamanEpicVest);
				}
				else if (m_questPlayer.CharacterClass.ID == (byte)eCharacterClass.Healer)
				{
					GiveItem(m_questPlayer, HealerEpicArms);
					GiveItem(m_questPlayer, HealerEpicBoots);
					GiveItem(m_questPlayer, HealerEpicGloves);
					GiveItem(m_questPlayer, HealerEpicHelm);
					GiveItem(m_questPlayer, HealerEpicLegs);
					GiveItem(m_questPlayer, HealerEpicVest);
				}

				m_questPlayer.GainExperience(eXPSource.Quest, 1937768448, true);
				//m_questPlayer.AddMoney(Money.GetMoney(0,0,0,2,Util.Random(50)), "You recieve {0} as a reward.");		
			}
			else
			{
				m_questPlayer.Out.SendMessage("You do not have enough free space in your inventory!", eChatType.CT_Important, eChatLoc.CL_SystemWindow);
			}
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
            *Valhalla Touched Boots 
            *Valhalla Touched Coif
            *Valhalla Touched Gloves
            *Valhalla Touched Hauberk
            *Valhalla Touched Legs
            *Valhalla Touched Sleeves
            *Subterranean Boots
            *Subterranean Coif
            *Subterranean Gloves
            *Subterranean Hauberk
            *Subterranean Legs
            *Subterranean Sleeves
        */

		#endregion
	}
}
