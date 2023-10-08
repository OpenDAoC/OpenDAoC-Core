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

// MarketSearch by Tolakram.  Donated from Storm
// Based on MarketNPC by Etaew, rewritten by Tolakram for new Inventory system


using System.Collections.Generic;
using DOL.Database;
using log4net;

namespace DOL.GS
{
	public class MarketSearch
	{
		private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

		public struct SearchData
		{
			public string name;
			public int slot;

			public int bonus1;
			public int bonus1Value;
			public int bonus2;
			public int bonus2Value;
			public int bonus3;
			public int bonus3Value;

			//public int skill;
			//public int resist;
			//public int bonus;
			//public int hp;
			//public int power;
			public int proc;
			public byte armorType;
			public byte damageType;
			public int levelMin;
			public int levelMax;
			public int minQual;
			//public int qtyMax;
			public uint priceMin;
			public uint priceMax;
			public byte playerCrafted;
			public int visual;
			public byte page;
			
			
			
			public string clientVersion;


            #region Market Explorer Packets Debug
            /*

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Slot
			// -------------------------------------------------------------------------
			// 0 - Any
			// 8 - Arms
			// 6 - Cloak
			// 3 - Feet
			// 2 - Hands
			// 1 - Head
			// 104 - Instrument
			// 4 - Jewel
			// 101 - Left Hand
			// 7 - Legs
			// 17 - Mythical
			// 9 - Neck
			// 100 - One Handed
			// 103 - Ranged
			// 15 - Ring
			// 105 - Shield
			// 5 - Torso
			// 102 - Two Handed
			// 12 - Waist
			// 106 - Wieldable
			// 13 - Wrist



			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Item Abilities
			// -------------------------------------------------------------------------
			// 0 - Any
			// 17 - Accuracy Boost
			// 2 - Add Spell Radius
			// 9 - Arcane Leadership
			// 5 - Arch Magery
			// 113 - Armor Factor Buff
			// 88 - Armor Factor Debuff
			// 6 - Armor Whiter
			// 80 - Arrogance
			// 7 - Arrogance (Invocation)
			// 18 - Attack Speed Decrease
			// 10 - Aura of Kings
			// 12 - Bedazzled
			// 19 - Bladeturn
			// 20 - Block/Parry Boost
			// 13 - Bolt
			// 3 - Boon of Kings
			// 15 - Buff Shear
			// 35 - Chained Direct Damage
			// 11 - Cheat Death
			// 30 - Comprehension Boost
			// 110 - Cooldown reset
			// 31 - Cost Reduction
			// 74 - Create Item
			// 109 - Cure Mesmerize
			// 44 - Damage Add
			// 38 - Damage Conversion
			// 45 - Damage Reduction
			// 47 - Damage Shield
			// 48 - Damage to Power
			// 64 - Decrease Casting Speed
			// 39 - Defensive Proc
			// 40 - Direct Damage
			// 42 - Disarmed
			// 43 - Disease
			// 82 - Disoriented
			// 86 - Dmg W/Resist Decrease
			// 49 - Damage Over Time
			// 81 - Effectiveness Debuff
			// 53 - Efficient Endurance
			// 54 - Efficient Healing
			// 55 - Enchanter Pet Boost
			// 56 - Endurance Drain
			// 61 - Endurance Heal
			// 58 - Endurance Regen
			// 21 - Evade Boost
			// 22 - Haste
			// 66 - Heal
			// 107 - Heal Over Time
			// 67 - Heal Bonus
			// 106 - Heal Hits Over Time
			// 59 - Health Regen
			// 23 - Ignore Bladeturn
			// 72 - Illusion
			// 94 - Improved Stat Decrease
			// 122 - Improved Stat Enhancements
			// 65 - Increase Attack Speed
			// 33 - Increase Fumble Chance
			// 35 - Increase Mellee Critical
			// 36 - Increase Spell Critical
			// 75 - Level effectiveness
			// 76 - Lifedrain
			// 79 - Lore Debuff
			// 70 - Magic Health Buffer
			// 1 - Mellee Absorption
			// 78 - Mellee Absorsion Debuff
			// 37 - Mellee Damage Boost
			// 24 - Mesmerization Feed
			// 25 - Mesmerize
			// 77 - Mesmerize Duration
			// 96 - Offensive Proc
			// 98 - Omni-Heal
			// 97 - Omni-Lifedrain
			// 26 - Parry
			// 100 - Pet Cast
			// 32 - Pet Scare
			// 71 - Physical Health Buffer
			// 60 - Power Regen
			// 102 - Power Transfer
			// 103 - Powershield
			// 4 - Raise Dead
			// 90 - Rampage
			// 104 - Realm Lore
			// 105 - Recovery
			// 108 - Remove Negative Effect
			// 101 - Replenish Power
			// 111 - Reset Quickcast
			// 27 - Resistence Bonus
			// 87 - Resistence Decrease
			// 112 - Resistance Enhance
			// 123 - Reward Bonus
			// 85 - Shatter illusion
			// 46 - Shield/Damage Return
			// 114 - Siege Lore
			// 116 - Speed Decrease
			// 117 - Speed Decrease W/Debuff
			// 28 - Speed Enhancement
			// 118 - Spell Pulse
			// 119 - Spreadheal
			// 89 - Stat Decrease
			// 92 - Stat Drain
			// 120 - Stat Enhancement
			// 73 - Stealth
			// 84 - Stealth Lore
			// 99 - Stun
			// 29 - Stun Feedback
			// 93 - Style Damage Shield
			// 50 - Summon
			// 51 - Summon Elemental
			// 52 - Summon Turret
			// 121 - Tempest
			// 91 - Vision of Malice
			// 14 - Water Breathing
			// 68 - Wave of Healing
			// 115 - Weaponskill Buff
			// 95 - Weaponskill Debuff
			// 62 - Weight of a Feather
			
			 
			 
			 
			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Damage Type 
			// -------------------------------------------------------------------------
			// 0 - Any
			// 1 - Crush
			// 2 - Slash
			// 3 - Thrust

			
			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Armor Class  
			// -------------------------------------------------------------------------
			// 0 - Any
			// 1 - Cloth
			// 2 - Leather
			// 3 - Studded
			// 4 - Chain
			// 5 - Plate
			// 6 - Reinforced
			// 7 - Scale


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses  
			// -------------------------------------------------------------------------
			// 0 - Any
			// 27 - Archery Damage
			// 20 - Archery Haste
			// 35 - Artifact
			// 50 - Bladeturn Reinforce
			// 52 - Block (PvE Only)
			// 22 - Bonus AF
			// 14 - Buff Bonus
			// 38 - Concentration
			// 26 - Craft Skill Gain
			// 25 - Craft Speed
			// 43 - Damage Cap Reduction
			// 44 - Death Experience Lost
			// 15 - Debuff Bonus
			// 49 - Defensive Bonus (PvE Only)
			// 17 - Endurance
			// 53 - Evade (PvE Only)
			// 31 - Fatigue Cap
			// 6 - Focus
			// 16 - Heal Bonus
			// 40 - Health Regeneration
			// 4 - Hits
			// 29 - Hits Cap
			// 8 - Mellee Damage
			// 19 - Mellee Haste
			// 62 - Mythical Block
			// 63 - Mythical Coin
			// 66 - Mythical Crowd Cont
			// 71 - Mythical DPS
			// 55 - Mythical Encumbrance
			// 78 - Mythical Endurance Regen
			// 67 - Mythical Essence Re
			// 61 - Mythical Evade
			// 76 - Mythical Health Regeneration
			// 60 - Mythical Parry
			// 80 - Mythical Physical Defense
			// 77 - Mythical Power Regen
			// 72 - Mythical Realm Points
			// 57 - Mythical Resist Cap
			// 68 - Mythical Resist and ??
			// 74 - Mythical Ressurection 
			// 70 - Mythical Run Speed
			// 79 - Mythical Safe Fall
			// 69 - Mythical Siege Damage
			// 58 - Mythical Siege Speed
			// 73 - Mythical Spell Increase
			// 64 - Mythical Stat Cap
			// 75 - Mythical Stat and Ca ??
			// 65 - Mythical Water Breathing
			// 56 - Mythical Water Movement
			// 46 - Negative Effect Duration
			// 51 - Parry (PvE Only)
			// 42 - Piece Ablative (PvE Only)
			// 3 - Power
			// 34 - Power %
			// 30 - Power Cap
			// 41 - Power Regeneration
			// 54 - Reactionary Style Damage
			// 5 - Resist
			// 2 - Skill
			// 9 - Spell Damage
			// 13 - Spell Duration
			// 21 - Spell Haste
			// 32 - Spell Piercing
			// 37 - Spell Power Cost Regeneration
			// 12 - Spell Range
			// 1 - Stat
			// 28 - Stats Cap
			// 47 - Style Cost Reduction
			// 10 - Style damage
			// 48 - To-Hit Bonus (PvE Only)
 
			 
			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Artifact 
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 1 - Arcane Siphon
			// 7 - Bonus BP
			// 5 - Bonus Gold
			// 6 - Bonus RP
			// 4 - Bonus XP
			// 2 - Conversion
			// 3 - Radiant Aura
			 

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Craft Skill Gain 
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 4 - Alchemy
			// 2 - Armorcrafting
			// 12 - Fletching
			// 13 - Spellcraft
			// 11 - Tailoring
			// 1 - Weaponcrafting


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Craft Speed 
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 4 - Alchemy
			// 2 - Armorcrafting
			// 12 - Fletching
			// 13 - Spellcraft
			// 11 - Tailoring
			// 1 - Weaponcrafting


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Focus (Albion)
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 304 - All Spell Lines
			// 72 - Body Magic
			// 68 - Cold Magic
			// 122 - Death Servant
			// 120 - Deathsight
			// 69 - Earth Magic
			// 66 - Fire Magic
			// 71 - Matter Magic
			// 74 - Mind Magic
			// 121 - Painworking
			// 73 - Spirit Magic
			// 67 - Wind Magic

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Mythical Resist Cap
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 16 - Body
			// 12 - Cold
			// 1 - Crush
			// 22 - Energy
			// 10 - Heat
			// 15 - Matter
			// 2 - Slash
			// 17 - Spirit
			// 3 - Thrust
			

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Mythical Resist And ???
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 16 - Body
			// 12 - Cold
			// 1 - Crush
			// 22 - Energy
			// 10 - Heat
			// 15 - Matter
			// 2 - Slash
			// 17 - Spirit
			// 3 - Thrust
			


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Mythical Stat Cap
			// ------------------------------------------------------------------------- 
			// 65535 - Any
			// 15 - All Stats
			// 10 - Acuity
			// 7 - Charisma
			// 2 - Constitution
			// 64 - Dexterity
			// 6 - Empathy
			// 4 - Intelligence
			// 5 - Piety
			// 3 - Quickness
			// 0 - Strength


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Mythical Stat And Ca???
			// ------------------------------------------------------------------------- 
			// 65535 - Any
			// 15 - All Stats
			// 10 - Acuity
			// 7 - Charisma
			// 2 - Constitution
			// 64 - Dexterity
			// 6 - Empathy
			// 4 - Intelligence
			// 5 - Piety
			// 3 - Quickness
			// 0 - Strength


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Resist
			// ------------------------------------------------------------------------- 
			// 0 - Any
			// 16 - Body
			// 12 - Cold
			// 1 - Crush
			// 22 - Energy
			// 10 - Heat
			// 15 - Matter
			// 2 - Slash
			// 17 - Spirit
			// 3 - Thrust


			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Skill (Albion)
			// -------------------------------------------------------------------------
			// 0 - Any
			// 302 - All Archery
			// 303 - All Casting
			// 301 - All Dual wield
			// 300 - All Primary Melee
			// 146 - Aura manipulation
			// 72 - Body Magic
			// 68 - Cold Magic
			// 118 - Critical Strinke
			// 91 - Crossbow
			// 33 - Crush
			// 122 - Death Servant
			// 120 - Deathsight
			// 77 - Dual wield
			// 69 - Earth Magic
			// 83 - Enhancements
			// 117 - Envenom
			// 66 - Fire Magic
			// 147 - Fist Wraps
			// 46 - Flex Weapons
			// 98 - Instruments
			// 145 - Magnetism
			// 71 - Matter Magic
			// 148 - Mauler Staff
			// 74 - Mind Magic
			// 121 - Painworking
			// 8 - Parry
			// 64 - Polearm
			// 144 - Power Strikes
			// 88 - Rejuvenation
			// 43 - Shield
			// 1 - Slash
			// 89 - Smiting
			// 123 - Soulrending
			// 73 - Spirit Magic
			// 47 - Staff
			// 19 - Stealth
			// 2 - Thrust
			// 65 - Two Handed
			// 67 - Wind Magic

			 
			*/
            #endregion


            public override string ToString()
			{
				//return string.Format("name:'{0}', slot:{1,2}, bonus1:{2,2}, bonus1value:{3,2}, bonus2:{4,2}, bonus2value:{5,2}, bonus3:{6,2}, bonus3value: {7,2} proc:{8}, armorType:{9,2}, damageType:{10}, levelMin:{11,2}, levelMax:{12,2}, qtyMin:{13,3}, priceMin:{14,2}, priceMax:{15,2}, playerCrafted:{16}, visual:{17}, clientVersion:{18}",
				//	name, slot, bonus1, bonus1Value, bonus2, bonus2Value, bonus3, bonus3Value, proc, armorType, damageType, levelMin, levelMax, qtyMin, priceMin, priceMax, playerCrafted, visual, clientVersion);
				//return string.Format("name:'{0}', slot:{1,2}, skill:{2,2}, resist:{3,2}, bonus:{4,2}, hp:{5,2}, power:{6,2}, proc:{7}, qtyMin:{8,3}, qtyMax:{9,3}, levelMin:{10,2}, levelMax:{11,2}, priceMin:{12,2}, priceMax:{13,2}, visual:{14}, armorType:{15:2}, damageType:{16}, playerCrafted:{17}, clientVersion:{18}",
				//	name, slot, skill, resist, bonus, hp, power, proc, qtyMin, qtyMax, levelMin, levelMax, priceMin, priceMax, visual, armorType, damageType, playerCrafted, clientVersion);
				return string.Format("name:'{0}' | slot:{1,2} | bonus1:{2,2} | bonus1value:{3,2} | bonus2:{4,2} | bonus2value:{5,2} | bonus3:{6,2} | bonus3value: {7,2} | proc:{8} | armorType:{9,2} | damageType:{10} | levelMin:{11,2} | levelMax:{12,2} | minQual:{13,3} | priceMin:{14,2} | priceMax:{15,2} | playerCrafted:{16} | visual:{17} | clientVersion:{18}",
					name, slot, bonus1, bonus1Value, bonus2, bonus2Value, bonus3, bonus3Value, proc, armorType, damageType, levelMin, levelMax, minQual, priceMin, priceMax, playerCrafted, visual, clientVersion);
			}
		}

		protected GamePlayer m_searchPlayer;
		protected bool m_searchHasError = false;

		public MarketSearch(GamePlayer player)
		{
			m_searchPlayer = player;
		}

		public virtual List<DbInventoryItem> FindItemsInList(IList<DbInventoryItem> inventoryItems, SearchData search)
		{
			List<DbInventoryItem> items = new List<DbInventoryItem>();

			int count = 0;
			m_searchHasError = false;

			if (ServerProperties.Properties.MARKET_ENABLE_LOG && search.page == 0)
			{
				log.DebugFormat("MarketSearch: [{0}:{1}] SEARCHING MARKET: {2}", m_searchPlayer.Name, m_searchPlayer.Client.Account.Name, search);
			}

			foreach (DbInventoryItem item in inventoryItems)
			{
				
				if (ServerProperties.Properties.MARKET_ENABLE_LOG)
				{
					log.DebugFormat("Analyzing cached item {0} to see if it fits search criteria.", item.Name);
				}


				// Error checking
				if (m_searchHasError)
					break;

				if (item.IsTradable == false)
					continue;

				if (item.SellPrice < 1)
					continue;

				if (item.OwnerLot == 0)
					continue;

				// ------------------------------------------------------------------------
				// search criteria

				if (search.name != "" && item.Name.ToLower().Contains(search.name.ToLower()) == false)
					continue;

				if (search.levelMin > 1 && item.Level < search.levelMin)
					continue;

				if (search.levelMax > -1 && search.levelMax < 52 && item.Level > search.levelMax)
					continue;

				if (search.minQual > 84 && item.Quality < search.minQual)
					continue;

				//if (search.qtyMax < 100 && item.Quality > search.qtyMax)
				//	continue;

				if (search.priceMin > 1 && item.SellPrice < search.priceMin)
					continue;

				if (search.priceMax > 0 && item.SellPrice > search.priceMax)
					continue;

				if (search.visual > 0 && item.Effect == 0)
					continue;

				if (search.playerCrafted > 0 && item.IsCrafted == false)
					continue;

				if (search.proc > 0 && item.ProcSpellID == 0 && item.ProcSpellID1 == 0)
					continue;

                if (CheckSlot(item, search.slot) == false)
                    continue;

                if (search.armorType > 0 && CheckForArmorType(item, search.armorType) == false)
					continue;

				// ------------------------------------------------------------------------
				//Stats
				if (search.bonus1 > 0 && search.bonus1 == 1 && CheckForBonus(item, search.bonus1Value) == false)
					continue;
				if (search.bonus2 > 0 && search.bonus2 == 1 && CheckForBonus(item, search.bonus2Value) == false)
					continue;
				if (search.bonus3 > 0 && search.bonus3 == 1 && CheckForBonus(item, search.bonus3Value) == false)
					continue;

				// ------------------------------------------------------------------------
				//Skills
				if (search.bonus1 > 0 && search.bonus1 == 2 && CheckForSkill(item, search.bonus1Value) == false)
					continue;
				if (search.bonus2 > 0 && search.bonus2 == 2 && CheckForSkill(item, search.bonus2Value) == false)
					continue;
				if (search.bonus3 > 0 && search.bonus3 == 2 && CheckForSkill(item, search.bonus3Value) == false)
					continue;

				// ------------------------------------------------------------------------
				//Power
				if (search.bonus1 > 0 && search.bonus1 == 3 && CheckForPower(item, search.bonus1Value) == false)
					continue;
				if (search.bonus2 > 0 && search.bonus2 == 3 && CheckForPower(item, search.bonus2Value) == false)
					continue;
				if (search.bonus3 > 0 && search.bonus3 == 3 && CheckForPower(item, search.bonus3Value) == false)
					continue;

				// ------------------------------------------------------------------------
				//MaxHP
				if (search.bonus1 > 0 && search.bonus1 == 4 && CheckForHP(item, search.bonus1Value) == false)
					continue;
				if (search.bonus2 > 0 && search.bonus2 == 4 && CheckForHP(item, search.bonus2Value) == false)
					continue;
				if (search.bonus3 > 0 && search.bonus3 == 4 && CheckForHP(item, search.bonus3Value) == false)
					continue;

				// ------------------------------------------------------------------------
				//Resists
				if (search.bonus1 > 0 && search.bonus1 == 5 && CheckForResist(item, search.bonus1Value) == false)
					continue;
				if (search.bonus2 > 0 && search.bonus2 == 5 && CheckForResist(item, search.bonus2Value) == false)
					continue;
				if (search.bonus3 > 0 && search.bonus3 == 5 && CheckForResist(item, search.bonus3Value) == false)
					continue;

				//if (search.hp > 0 && CheckForHP(item, search.hp) == false)
				//	continue;

				//if (search.power > 0 && CheckForPower(item, search.power) == false)
				//	continue;

				//if (search.bonus >= 0 && CheckForBonus(item, search.bonus) == false)
				//	continue;

				//if (search.skill >= 0 && CheckForSkill(item, search.skill) == false)
				//	continue;

				//if (search.resist >= 0 && CheckForResist(item, search.resist) == false)
				//	continue;

				// ------------------------------------------------------------------------
				// Damage Type
				if (search.damageType > 0 && CheckForDamageType(item, search.damageType) == false)
					continue;

				if (ServerProperties.Properties.MARKET_ENABLE_LOG)
				{
					log.DebugFormat("Adding item '{0}' to the list of search ...", item.Name);
				}
				
				items.Add(item);

				if (++count >= ServerProperties.Properties.MARKET_SEARCH_LIMIT)
					break;
			}

			if (ServerProperties.Properties.MARKET_ENABLE_LOG)
			{
				log.DebugFormat("Returning '{0}' items from search.", items.Count);
			}

			return items;
		}

		protected virtual bool CheckSlot(DbInventoryItem item, int slot)
		{
		
			if (ServerProperties.Properties.MARKET_ENABLE_LOG)
			{
				log.DebugFormat("Market Explorer Search for items on slot '{0}' and for item '{1}'", slot, item.Name);
				log.DebugFormat("Market Explorer Search current item '{0}' as itemType '{1}' comparing with '{2}'.", item.Name, item.Item_Type, (int)eInventorySlot.ArmsArmor);
				log.DebugFormat("Market Explorer Search for items on slot '{0}' and for item.Type '{1}'.", slot, item.Item_Type);
			}

			if (slot != -1)
			{
				switch (slot)
				{
					// 8 - Arms
					// 6 - Cloak
					// 3 - Feet
					// 2 - Hands
					// 1 - Head
					// 104 - Instrument
					// 4 - Jewel
					// 101 - Left Hand
					// 7 - Legs
					// 17 - Mythical
					// 9 - Neck
					// 100 - One Handed
					// 103 - Ranged
					// 15 - Ring
					// 105 - Shield
					// 5 - Torso
					// 102 - Two Handed
					// 12 - Waist
					// 106 - Wieldable
					// 13 - Wrist

					case 1: return item.Item_Type == (int)eInventorySlot.HeadArmor;
					case 2: return item.Item_Type == (int)eInventorySlot.HandsArmor;
					case 3: return item.Item_Type == (int)eInventorySlot.FeetArmor;
					case 4: return item.Object_Type == (int)EObjectType.Magical && item.Item_Type == (int)eInventorySlot.Jewellery;
					case 5: return item.Item_Type == (int)eInventorySlot.TorsoArmor;
					case 6: return item.Object_Type == (int)EObjectType.Magical && item.Item_Type == (int)eInventorySlot.Cloak;
					case 7: return item.Item_Type == (int)eInventorySlot.LegsArmor;
					case 8: return item.Item_Type == (int)eInventorySlot.ArmsArmor;
					case 9: return item.Object_Type == (int)EObjectType.Magical && item.Item_Type == (int)eInventorySlot.Neck;
					case 12: return item.Object_Type == (int)EObjectType.Magical && item.Item_Type == (int)eInventorySlot.Waist;
					case 13: return item.Object_Type == (int)EObjectType.Magical && (item.Item_Type == (int)eInventorySlot.RightBracer || item.Item_Type == (int)eInventorySlot.LeftBracer);
					case 15: return item.Object_Type == (int)EObjectType.Magical && (item.Item_Type == (int)eInventorySlot.RightRing || item.Item_Type == (int)eInventorySlot.LeftRing);					
					case 100: return item.Item_Type == (int)eInventorySlot.RightHandWeapon || item.Item_Type == (int)eInventorySlot.LeftHandWeapon || item.Item_Type == (int)eInventorySlot.TwoHandWeapon;
					case 101: return item.Item_Type == (int)eInventorySlot.LeftHandWeapon;
					case 102: return item.Item_Type == (int)eInventorySlot.TwoHandWeapon;
					case 103: return item.Item_Type == (int)eInventorySlot.DistanceWeapon;
					case 104: return item.Object_Type == (int)EObjectType.Instrument && item.Item_Type == (int)eInventorySlot.RightHandWeapon;
					case 105: return item.Object_Type == (int)EObjectType.Shield && item.Item_Type == (int)eInventorySlot.LeftHandWeapon;					
					case 106: return item.Object_Type == (int)EObjectType.GenericItem;

					default:

						//log.Error("There has been an unexpected slot passed to CheckSlot: " + slot);
						//ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled slot (" + slot + ") specified in search!");
						//m_searchHasError = true;
						break;

				}
			}

			return true;
		}

		protected virtual bool CheckForHP(DbInventoryItem item, int hp)
		{
			if (hp > 0)
			{
				if ((item.ExtraBonusType == (int)EProperty.MaxHealth && item.ExtraBonus >= hp) ||
					(item.Bonus1Type == (int)EProperty.MaxHealth && item.Bonus1 >= hp) ||
					(item.Bonus2Type == (int)EProperty.MaxHealth && item.Bonus2 >= hp) ||
					(item.Bonus3Type == (int)EProperty.MaxHealth && item.Bonus3 >= hp) ||
					(item.Bonus4Type == (int)EProperty.MaxHealth && item.Bonus4 >= hp) ||
					(item.Bonus5Type == (int)EProperty.MaxHealth && item.Bonus5 >= hp) ||
					(item.Bonus6Type == (int)EProperty.MaxHealth && item.Bonus6 >= hp) ||
					(item.Bonus7Type == (int)EProperty.MaxHealth && item.Bonus7 >= hp) ||
					(item.Bonus8Type == (int)EProperty.MaxHealth && item.Bonus8 >= hp) ||
					(item.Bonus9Type == (int)EProperty.MaxHealth && item.Bonus9 >= hp) ||
					(item.Bonus10Type == (int)EProperty.MaxHealth && item.Bonus10 >= hp))
					return true;
			}

			return false;
		}

		protected virtual bool CheckForPower(DbInventoryItem item, int power)
		{
			if (power > 0)
			{
				if ((item.ExtraBonusType == (int)EProperty.MaxMana && item.ExtraBonus >= power) ||
					(item.Bonus1Type == (int)EProperty.MaxMana && item.Bonus1 >= power) ||
					(item.Bonus2Type == (int)EProperty.MaxMana && item.Bonus2 >= power) ||
					(item.Bonus3Type == (int)EProperty.MaxMana && item.Bonus3 >= power) ||
					(item.Bonus4Type == (int)EProperty.MaxMana && item.Bonus4 >= power) ||
					(item.Bonus5Type == (int)EProperty.MaxMana && item.Bonus5 >= power) ||
					(item.Bonus6Type == (int)EProperty.MaxMana && item.Bonus6 >= power) ||
					(item.Bonus7Type == (int)EProperty.MaxMana && item.Bonus7 >= power) ||
					(item.Bonus8Type == (int)EProperty.MaxMana && item.Bonus8 >= power) ||
					(item.Bonus9Type == (int)EProperty.MaxMana && item.Bonus9 >= power) ||
					(item.Bonus10Type == (int)EProperty.MaxMana && item.Bonus10 >= power))
					return true;
			}

			return false;
		}

		protected virtual bool CheckForArmorType(DbInventoryItem item, int type)
		{
			switch (type)
			{
				case 1:

					return item.Object_Type == (int)EObjectType.Cloth;

				case 2:

					return item.Object_Type == (int)EObjectType.Leather;

				case 3:

					return item.Object_Type == (int)EObjectType.Studded;

				case 4:

					return item.Object_Type == (int)EObjectType.Chain;

				case 5:

					return item.Object_Type == (int)EObjectType.Plate;

				case 6:

					return item.Object_Type == (int)EObjectType.Reinforced;

				case 7:

					return item.Object_Type == (int)EObjectType.Scale;

				default:

					log.Error("There has been an unexpected type passed to CheckForArmorType: " + type);
					ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled armor type (" + type + ") specified in search!");
					//m_searchHasError = true;
					return false;
			}
		}

		protected virtual bool CheckForResist(DbInventoryItem item, int resist)
		{
			switch (resist)
			{
				case 0: return CheckForProperty(item, (int)EProperty.Resist_Body);
				case 1: return CheckForProperty(item, (int)EProperty.Resist_Cold);
				case 2: return CheckForProperty(item, (int)EProperty.Resist_Heat);
				case 3: return CheckForProperty(item, (int)EProperty.Resist_Energy);
				case 4: return CheckForProperty(item, (int)EProperty.Resist_Matter);
				case 5: return CheckForProperty(item, (int)EProperty.Resist_Spirit);
				case 6: return CheckForProperty(item, (int)EProperty.Resist_Thrust);
				case 7: return CheckForProperty(item, (int)EProperty.Resist_Crush);
				case 8: return CheckForProperty(item, (int)EProperty.Resist_Slash);

				default:

					log.Error("There has been an unexpected resist passed to CheckForResist: " + resist);
					ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled resist (" + resist + ") specified in search!");
					//m_searchHasError = true;
					break;
			}

			return false;
		}


		protected virtual bool CheckForSkill(DbInventoryItem item, int skill)
		{
			if (skill > 0)
			{
				switch (skill)
				{
					case 1: return CheckForProperty(item, (int)EProperty.Skill_Slashing);
					case 2: return CheckForProperty(item, (int)EProperty.Skill_Thrusting);
					case 8: return CheckForProperty(item, (int)EProperty.Skill_Parry);
					case 14: return CheckForProperty(item, (int)EProperty.Skill_Sword);
					case 16: return CheckForProperty(item, (int)EProperty.Skill_Hammer);
					case 17: return CheckForProperty(item, (int)EProperty.Skill_Axe);
					case 18: return CheckForProperty(item, (int)EProperty.Skill_Left_Axe);
					case 19: return CheckForProperty(item, (int)EProperty.Skill_Stealth);
					case 26: return CheckForProperty(item, (int)EProperty.Skill_Spear);
					case 29: return CheckForProperty(item, (int)EProperty.Skill_Mending);
					case 30: return CheckForProperty(item, (int)EProperty.Skill_Augmentation);
					case 33: return CheckForProperty(item, (int)EProperty.Skill_Crushing);
					case 34: return CheckForProperty(item, (int)EProperty.Skill_Pacification);
					case 37: return CheckForProperty(item, (int)EProperty.Skill_Subterranean); // this is cave magic
					case 38: return CheckForProperty(item, (int)EProperty.Skill_Darkness);
					case 39: return CheckForProperty(item, (int)EProperty.Skill_Suppression);
					case 42: return CheckForProperty(item, (int)EProperty.Skill_Runecarving);
					case 43: return CheckForProperty(item, (int)EProperty.Skill_Shields);
					case 46: return CheckForProperty(item, (int)EProperty.Skill_Flexible_Weapon);
					case 47: return CheckForProperty(item, (int)EProperty.Skill_Staff);
					case 48: return CheckForProperty(item, (int)EProperty.Skill_Summoning);
					case 50: return CheckForProperty(item, (int)EProperty.Skill_Stormcalling);
					case 62: return CheckForProperty(item, (int)EProperty.Skill_BeastCraft);
					case 64: return CheckForProperty(item, (int)EProperty.Skill_Polearms);
					case 65: return CheckForProperty(item, (int)EProperty.Skill_Two_Handed);
					case 66: return CheckForProperty(item, (int)EProperty.Skill_Fire);
					case 67: return CheckForProperty(item, (int)EProperty.Skill_Wind);
					case 68: return CheckForProperty(item, (int)EProperty.Skill_Cold);
					case 69: return CheckForProperty(item, (int)EProperty.Skill_Earth);
					case 70: return CheckForProperty(item, (int)EProperty.Skill_Light);
					case 71: return CheckForProperty(item, (int)EProperty.Skill_Matter);
					case 72: return CheckForProperty(item, (int)EProperty.Skill_Body);
					case 73: return CheckForProperty(item, (int)EProperty.Skill_Spirit);
					case 74: return CheckForProperty(item, (int)EProperty.Skill_Mind);
					case 75: return CheckForProperty(item, (int)EProperty.Skill_Void);
					case 76: return CheckForProperty(item, (int)EProperty.Skill_Mana);
					case 77: return CheckForProperty(item, (int)EProperty.Skill_Dual_Wield);
					case 78: return CheckForProperty(item, (int)EProperty.Skill_Archery); // was composite bow
					case 82: return CheckForProperty(item, (int)EProperty.Skill_Battlesongs);
					case 83: return CheckForProperty(item, (int)EProperty.Skill_Enhancement);
					case 84: return CheckForProperty(item, (int)EProperty.Skill_Enchantments);
					case 88: return CheckForProperty(item, (int)EProperty.Skill_Rejuvenation);
					case 89: return CheckForProperty(item, (int)EProperty.Skill_Smiting);

					case 90: return CheckForProperty(item, (int)EProperty.Skill_Long_bows) || 
									CheckForProperty(item, (int)EProperty.Skill_Composite) || 
									CheckForProperty(item, (int)EProperty.Skill_RecurvedBow); //  archery?  

					case 91: return CheckForProperty(item, (int)EProperty.Skill_Cross_Bows);
					case 97: return CheckForProperty(item, (int)EProperty.Skill_Chants); // chants?
					case 98: return CheckForProperty(item, (int)EProperty.Skill_Instruments);
					case 101: return CheckForProperty(item, (int)EProperty.Skill_Blades);
					case 102: return CheckForProperty(item, (int)EProperty.Skill_Blunt);
					case 103: return CheckForProperty(item, (int)EProperty.Skill_Piercing);
					case 104: return CheckForProperty(item, (int)EProperty.Skill_Large_Weapon);
					case 105: return CheckForProperty(item, (int)EProperty.Skill_Mentalism);
					case 106: return CheckForProperty(item, (int)EProperty.Skill_Regrowth);
					case 107: return CheckForProperty(item, (int)EProperty.Skill_Nurture);
					case 108: return CheckForProperty(item, (int)EProperty.Skill_Nature);
					case 109: return CheckForProperty(item, (int)EProperty.Skill_Music);
					case 110: return CheckForProperty(item, (int)EProperty.Skill_Celtic_Dual);
					case 112: return CheckForProperty(item, (int)EProperty.Skill_Celtic_Spear);
					case 113: return CheckForProperty(item, (int)EProperty.Skill_Archery); // was recurve bow
					case 114: return CheckForProperty(item, (int)EProperty.Skill_Valor);
					case 116: return CheckForProperty(item, (int)EProperty.Skill_Pathfinding);
					case 117: return CheckForProperty(item, (int)EProperty.Skill_Envenom);
					case 118: return CheckForProperty(item, (int)EProperty.Skill_Critical_Strike);
					case 120: return CheckForProperty(item, (int)EProperty.Skill_DeathSight);
					case 121: return CheckForProperty(item, (int)EProperty.Skill_Pain_working);
					case 122: return CheckForProperty(item, (int)EProperty.Skill_Death_Servant);
					case 123: return CheckForProperty(item, (int)EProperty.Skill_SoulRending);
					case 124: return CheckForProperty(item, (int)EProperty.Skill_HandToHand);
					case 125: return CheckForProperty(item, (int)EProperty.Skill_Scythe);
					case 126: return CheckForProperty(item, (int)EProperty.Skill_BoneArmy);
					case 127: return CheckForProperty(item, (int)EProperty.Skill_Arboreal);
					case 129: return CheckForProperty(item, (int)EProperty.Skill_Creeping);
					case 130: return CheckForProperty(item, (int)EProperty.Skill_Verdant);
					case 133: return CheckForProperty(item, (int)EProperty.Skill_OdinsWill);
					case 134: return CheckForProperty(item, (int)EProperty.Skill_SpectralGuard) || CheckForProperty(item, (int)EProperty.Skill_SpectralForce);
					case 135: return CheckForProperty(item, (int)EProperty.Skill_PhantasmalWail);
					case 136: return CheckForProperty(item, (int)EProperty.Skill_EtherealShriek);
					case 137: return CheckForProperty(item, (int)EProperty.Skill_ShadowMastery);
					case 138: return CheckForProperty(item, (int)EProperty.Skill_VampiiricEmbrace);
					case 139: return CheckForProperty(item, (int)EProperty.Skill_Dementia);
					case 140: return CheckForProperty(item, (int)EProperty.Skill_Witchcraft);
					case 141: return CheckForProperty(item, (int)EProperty.Skill_Cursing);
					case 142: return CheckForProperty(item, (int)EProperty.Skill_Hexing);
					case 147: return CheckForProperty(item, (int)EProperty.Skill_FistWraps);
					case 148: return CheckForProperty(item, (int)EProperty.Skill_MaulerStaff);

					case 300: return CheckForProperty(item, (int)EProperty.Skill_Slashing) ||
										CheckForProperty(item, (int)EProperty.Skill_Thrusting) ||
										CheckForProperty(item, (int)EProperty.Skill_Crushing) ||
										CheckForProperty(item, (int)EProperty.Skill_Shields) ||
										CheckForProperty(item, (int)EProperty.Skill_Flexible_Weapon) ||
										CheckForProperty(item, (int)EProperty.Skill_Staff) ||
										CheckForProperty(item, (int)EProperty.Skill_Polearms) ||
										CheckForProperty(item, (int)EProperty.Skill_Two_Handed) ||
										CheckForProperty(item, (int)EProperty.Skill_Dual_Wield) ||
										CheckForProperty(item, (int)EProperty.Skill_Long_bows) ||
										CheckForProperty(item, (int)EProperty.Skill_Composite) ||
										CheckForProperty(item, (int)EProperty.Skill_RecurvedBow) ||
										CheckForProperty(item, (int)EProperty.Skill_Cross_Bows) ||
										CheckForProperty(item, (int)EProperty.Skill_FistWraps) ||
										CheckForProperty(item, (int)EProperty.Skill_Sword) ||
										CheckForProperty(item, (int)EProperty.Skill_Hammer) ||
										CheckForProperty(item, (int)EProperty.Skill_Axe) ||
										CheckForProperty(item, (int)EProperty.Skill_Left_Axe) ||
										CheckForProperty(item, (int)EProperty.Skill_Spear) ||
										CheckForProperty(item, (int)EProperty.Skill_Archery) ||
										CheckForProperty(item, (int)EProperty.Skill_Blades) ||
										CheckForProperty(item, (int)EProperty.Skill_Blunt) ||
										CheckForProperty(item, (int)EProperty.Skill_Piercing) ||
										CheckForProperty(item, (int)EProperty.Skill_Large_Weapon) ||
										CheckForProperty(item, (int)EProperty.Skill_Celtic_Dual) ||
										CheckForProperty(item, (int)EProperty.Skill_Celtic_Spear) ||
										CheckForProperty(item, (int)EProperty.Skill_Critical_Strike) ||
										CheckForProperty(item, (int)EProperty.Skill_HandToHand) ||
										CheckForProperty(item, (int)EProperty.Skill_Archery) ||
										CheckForProperty(item, (int)EProperty.Skill_Scythe) ||
										CheckForProperty(item, (int)EProperty.AllMeleeWeaponSkills) ||
										CheckForProperty(item, (int)EProperty.AllDualWieldingSkills) ||
										CheckForProperty(item, (int)EProperty.AllArcherySkills) ||
										CheckForProperty(item, (int)EProperty.Skill_MaulerStaff);

					case 303: return CheckForProperty(item, (int)EProperty.Skill_Fire) ||
										CheckForProperty(item, (int)EProperty.Skill_Wind) ||
										CheckForProperty(item, (int)EProperty.Skill_Cold) ||
										CheckForProperty(item, (int)EProperty.Skill_Earth) ||
										CheckForProperty(item, (int)EProperty.Skill_Matter) ||
										CheckForProperty(item, (int)EProperty.Skill_Body) ||
										CheckForProperty(item, (int)EProperty.Skill_Spirit) ||
										CheckForProperty(item, (int)EProperty.Skill_Mind) ||
										CheckForProperty(item, (int)EProperty.Skill_Enhancement) ||
										CheckForProperty(item, (int)EProperty.Skill_Rejuvenation) ||
										CheckForProperty(item, (int)EProperty.Skill_Smiting) ||
										CheckForProperty(item, (int)EProperty.Skill_Chants) ||
										CheckForProperty(item, (int)EProperty.Skill_DeathSight) ||
										CheckForProperty(item, (int)EProperty.Skill_Pain_working) ||
										CheckForProperty(item, (int)EProperty.Skill_Death_Servant) ||
										CheckForProperty(item, (int)EProperty.Skill_SoulRending) ||
										CheckForProperty(item, (int)EProperty.Skill_Mending) ||
										CheckForProperty(item, (int)EProperty.Skill_Augmentation) ||
										CheckForProperty(item, (int)EProperty.Skill_Pacification) ||
										CheckForProperty(item, (int)EProperty.Skill_Subterranean) ||
										CheckForProperty(item, (int)EProperty.Skill_Darkness) ||
										CheckForProperty(item, (int)EProperty.Skill_Suppression) ||
										CheckForProperty(item, (int)EProperty.Skill_Runecarving) ||
										CheckForProperty(item, (int)EProperty.Skill_Summoning) ||
										CheckForProperty(item, (int)EProperty.Skill_Stormcalling) ||
										CheckForProperty(item, (int)EProperty.Skill_BeastCraft) ||
										CheckForProperty(item, (int)EProperty.Skill_Light) ||
										CheckForProperty(item, (int)EProperty.Skill_Void) ||
										CheckForProperty(item, (int)EProperty.Skill_Mana) ||
										CheckForProperty(item, (int)EProperty.Skill_Mentalism) ||
										CheckForProperty(item, (int)EProperty.Skill_Regrowth) ||
										CheckForProperty(item, (int)EProperty.Skill_Nurture) ||
										CheckForProperty(item, (int)EProperty.Skill_Nature) ||
										CheckForProperty(item, (int)EProperty.Skill_Music) ||
										CheckForProperty(item, (int)EProperty.Skill_Valor) ||
										CheckForProperty(item, (int)EProperty.Skill_Pathfinding) ||
										CheckForProperty(item, (int)EProperty.Skill_Envenom) ||
										CheckForProperty(item, (int)EProperty.Skill_BoneArmy) ||
										CheckForProperty(item, (int)EProperty.Skill_Arboreal) ||
										CheckForProperty(item, (int)EProperty.Skill_Creeping) ||
										CheckForProperty(item, (int)EProperty.Skill_Verdant) ||
										CheckForProperty(item, (int)EProperty.Skill_OdinsWill) ||
										CheckForProperty(item, (int)EProperty.Skill_SpectralGuard) ||
										CheckForProperty(item, (int)EProperty.Skill_SpectralForce) ||
										CheckForProperty(item, (int)EProperty.Skill_PhantasmalWail) ||
										CheckForProperty(item, (int)EProperty.Skill_EtherealShriek) ||
										CheckForProperty(item, (int)EProperty.Skill_ShadowMastery) ||
										CheckForProperty(item, (int)EProperty.Skill_VampiiricEmbrace) ||
										CheckForProperty(item, (int)EProperty.Skill_Dementia) ||
										CheckForProperty(item, (int)EProperty.Skill_Witchcraft) ||
										CheckForProperty(item, (int)EProperty.Skill_Cursing) ||
										CheckForProperty(item, (int)EProperty.Skill_Hexing) ||
										CheckForProperty(item, (int)EProperty.AllMagicSkills);

					default:

						log.Error("There has been an unexpected skill passed to CheckForSkill: " + skill);
						ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled skill (" + skill + ") specified in search!");
						//m_searchHasError = true;
						break;
				}

			}

			return false;
		}


		protected virtual bool CheckForBonus(DbInventoryItem item, int bonus)
		{
			if (bonus > 0)
			{
				switch (bonus)
				{
					case 0: return CheckForProperty(item, (int)EProperty.Strength);
					case 1: return CheckForProperty(item, (int)EProperty.Constitution);
					case 2: return CheckForProperty(item, (int)EProperty.Dexterity);
					case 3: return CheckForProperty(item, (int)EProperty.Quickness);
					case 4: return CheckForProperty(item, (int)EProperty.Piety);
					case 5: return CheckForProperty(item, (int)EProperty.Empathy);
					case 6: return CheckForProperty(item, (int)EProperty.Intelligence);
					case 7: return CheckForProperty(item, (int)EProperty.Charisma);

					case 8: return CheckForProperty(item, (int)EProperty.Dexterity) ||
									CheckForProperty(item, (int)EProperty.Intelligence) ||
									CheckForProperty(item, (int)EProperty.Charisma) ||
									CheckForProperty(item, (int)EProperty.Acuity) ||
									CheckForProperty(item, (int)EProperty.Empathy) ||
									CheckForProperty(item, (int)EProperty.Piety);

					case 9:
						{
							for (int i = (int)EProperty.ToABonus_First; i <= (int)EProperty.ToABonus_Last; i++)
							{
								if (CheckForProperty(item, i))
									return true;
							}

							return false;
						}

					case 10: return CheckForProperty(item, (int)EProperty.PowerPool);
					case 11: return CheckForProperty(item, (int)EProperty.HealingEffectiveness);
					case 12: return CheckForProperty(item, (int)EProperty.SpellDuration);
					case 13: return CheckForProperty(item, (int)EProperty.ArmorFactor);
					case 14: return CheckForProperty(item, (int)EProperty.CastingSpeed);
					case 15: return CheckForProperty(item, (int)EProperty.MeleeSpeed);
					case 16: return CheckForProperty(item, (int)EProperty.ArcherySpeed);

					case 17: return CheckForProperty(item, (int)EProperty.StrCapBonus) ||
									CheckForProperty(item, (int)EProperty.DexCapBonus) ||
									CheckForProperty(item, (int)EProperty.ConCapBonus) ||
									CheckForProperty(item, (int)EProperty.QuiCapBonus) ||
									CheckForProperty(item, (int)EProperty.IntCapBonus) ||
									CheckForProperty(item, (int)EProperty.PieCapBonus) ||
									CheckForProperty(item, (int)EProperty.EmpCapBonus) ||
									CheckForProperty(item, (int)EProperty.ChaCapBonus) ||
									CheckForProperty(item, (int)EProperty.AcuCapBonus) ||
									CheckForProperty(item, (int)EProperty.MaxHealthCapBonus) ||
									CheckForProperty(item, (int)EProperty.PowerPoolCapBonus);

					case 18: return CheckForProperty(item, (int)EProperty.MaxHealthCapBonus);
					case 19: return CheckForProperty(item, (int)EProperty.BuffEffectiveness);

					case 20:
						{
							// Catacombs - for DOL this is customized to special skills not included 
							// in other searches -Tolakram

							for (int i = (int)EProperty.MaxSpeed; i <= (int)EProperty.Acuity; i++)
							{
								if (CheckForProperty(item, i))
									return true;
							}

							for (int i = (int)EProperty.EvadeChance; i <= (int)EProperty.ToHitBonus; i++)
							{
								if (CheckForProperty(item, i))
									return true;
							}

							for (int i = (int)EProperty.DPS; i <= (int)EProperty.MaxProperty; i++)
							{
								if (CheckForProperty(item, i))
									return true;
							}

							return false;
						}

					default:

						log.Error("There has been an unexpected bonus passed to CheckForBonus: " + bonus);
						ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled bonus type (" + bonus + ") specified in search!");
						//m_searchHasError = true;
						break;
				}
			}

			return false;
		}

		protected virtual bool CheckForDamageType(DbInventoryItem item, int damageType)
		{
			if (GlobalConstants.IsWeapon(item.Object_Type))
			{
				if (damageType >= 1 && damageType <= 3)
				{
					return damageType == item.Type_Damage;
				}
				else
				{
					log.Error("There has been an unexpected bonus passed to CheckForDamageType: " + damageType);
					ChatUtil.SendErrorMessage(m_searchPlayer, "Unhandled damage type (" + damageType + ") specified in search!");
					//m_searchHasError = true;
					return false;
				}
			}

			return false;
		}


		protected virtual bool CheckForProperty(DbInventoryItem item, int property)
		{
			if (property > 0)
			{
				if ((item.ExtraBonusType == property && item.ExtraBonus >= 0) ||
					(item.Bonus1Type == property && item.Bonus1 >= 0) ||
					(item.Bonus2Type == property && item.Bonus2 >= 0) ||
					(item.Bonus3Type == property && item.Bonus3 >= 0) ||
					(item.Bonus4Type == property && item.Bonus4 >= 0) ||
					(item.Bonus5Type == property && item.Bonus5 >= 0) ||
					(item.Bonus6Type == property && item.Bonus6 >= 0) ||
					(item.Bonus7Type == property && item.Bonus7 >= 0) ||
					(item.Bonus8Type == property && item.Bonus8 >= 0) ||
					(item.Bonus9Type == property && item.Bonus9 >= 0) ||
					(item.Bonus10Type == property && item.Bonus10 >= 0))
					return true;
			}

			return false;
		}



		#region ALL Packet Enumerations

		/*

		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Slot
		// -------------------------------------------------------------------------
		// 0 - Any
		// 8 - Arms
		// 6 - Cloak
		// 3 - Feet
		// 2 - Hands
		// 1 - Head
		// 104 - Instrument
		// 4 - Jewel
		// 101 - Left Hand
		// 7 - Legs
		// 17 - Mythical
		// 9 - Neck
		// 100 - One Handed
		// 103 - Ranged
		// 15 - Ring
		// 105 - Shield
		// 5 - Torso
		// 102 - Two Handed
		// 12 - Waist
		// 106 - Wieldable
		// 13 - Wrist



		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Item Abilities
		// -------------------------------------------------------------------------
		// 0 - Any
		// 17 - Accuracy Boost
		// 2 - Add Spell Radius
		// 9 - Arcane Leadership
		// 5 - Arch Magery
		// 113 - Armor Factor Buff
		// 88 - Armor Factor Debuff
		// 6 - Armor Whiter
		// 80 - Arrogance
		// 7 - Arrogance (Invocation)
		// 18 - Attack Speed Decrease
		// 10 - Aura of Kings
		// 12 - Bedazzled
		// 19 - Bladeturn
		// 20 - Block/Parry Boost
		// 13 - Bolt
		// 3 - Boon of Kings
		// 15 - Buff Shear
		// 35 - Chained Direct Damage
		// 11 - Cheat Death
		// 30 - Comprehension Boost
		// 110 - Cooldown reset
		// 31 - Cost Reduction
		// 74 - Create Item
		// 109 - Cure Mesmerize
		// 44 - Damage Add
		// 38 - Damage Conversion
		// 45 - Damage Reduction
		// 47 - Damage Shield
		// 48 - Damage to Power
		// 64 - Decrease Casting Speed
		// 39 - Defensive Proc
		// 40 - Direct Damage
		// 42 - Disarmed
		// 43 - Disease
		// 82 - Disoriented
		// 86 - Dmg W/Resist Decrease
		// 49 - Damage Over Time
		// 81 - Effectiveness Debuff
		// 53 - Efficient Endurance
		// 54 - Efficient Healing
		// 55 - Enchanter Pet Boost
		// 56 - Endurance Drain
		// 61 - Endurance Heal
		// 58 - Endurance Regen
		// 21 - Evade Boost
		// 22 - Haste
		// 66 - Heal
		// 107 - Heal Over Time
		// 67 - Heal Bonus
		// 106 - Heal Hits Over Time
		// 59 - Health Regen
		// 23 - Ignore Bladeturn
		// 72 - Illusion
		// 94 - Improved Stat Decrease
		// 122 - Improved Stat Enhancements
		// 65 - Increase Attack Speed
		// 33 - Increase Fumble Chance
		// 35 - Increase Mellee Critical
		// 36 - Increase Spell Critical
		// 75 - Level effectiveness
		// 76 - Lifedrain
		// 79 - Lore Debuff
		// 70 - Magic Health Buffer
		// 1 - Mellee Absorption
		// 78 - Mellee Absorsion Debuff
		// 37 - Mellee Damage Boost
		// 24 - Mesmerization Feed
		// 25 - Mesmerize
		// 77 - Mesmerize Duration
		// 96 - Offensive Proc
		// 98 - Omni-Heal
		// 97 - Omni-Lifedrain
		// 26 - Parry
		// 100 - Pet Cast
		// 32 - Pet Scare
		// 71 - Physical Health Buffer
		// 60 - Power Regen
		// 102 - Power Transfer
		// 103 - Powershield
		// 4 - Raise Dead
		// 90 - Rampage
		// 104 - Realm Lore
		// 105 - Recovery
		// 108 - Remove Negative Effect
		// 101 - Replenish Power
		// 111 - Reset Quickcast
		// 27 - Resistence Bonus
		// 87 - Resistence Decrease
		// 112 - Resistance Enhance
		// 123 - Reward Bonus
		// 85 - Shatter illusion
		// 46 - Shield/Damage Return
		// 114 - Siege Lore
		// 116 - Speed Decrease
		// 117 - Speed Decrease W/Debuff
		// 28 - Speed Enhancement
		// 118 - Spell Pulse
		// 119 - Spreadheal
		// 89 - Stat Decrease
		// 92 - Stat Drain
		// 120 - Stat Enhancement
		// 73 - Stealth
		// 84 - Stealth Lore
		// 99 - Stun
		// 29 - Stun Feedback
		// 93 - Style Damage Shield
		// 50 - Summon
		// 51 - Summon Elemental
		// 52 - Summon Turret
		// 121 - Tempest
		// 91 - Vision of Malice
		// 14 - Water Breathing
		// 68 - Wave of Healing
		// 115 - Weaponskill Buff
		// 95 - Weaponskill Debuff
		// 62 - Weight of a Feather




		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Damage Type 
		// -------------------------------------------------------------------------
		// 0 - Any
		// 1 - Crush
		// 2 - Slash
		// 3 - Thrust


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Armor Class  
		// -------------------------------------------------------------------------
		// 0 - Any
		// 1 - Cloth
		// 2 - Leather
		// 3 - Studded
		// 4 - Chain
		// 5 - Plate
		// 6 - Reinforced
		// 7 - Scale


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses  
		// -------------------------------------------------------------------------
		// 0 - Any
		// 27 - Archery Damage
		// 20 - Archery Haste
		// 35 - Artifact
		// 50 - Bladeturn Reinforce
		// 52 - Block (PvE Only)
		// 22 - Bonus AF
		// 14 - Buff Bonus
		// 38 - Concentration
		// 26 - Craft Skill Gain
		// 25 - Craft Speed
		// 43 - Damage Cap Reduction
		// 44 - Death Experience Lost
		// 15 - Debuff Bonus
		// 49 - Defensive Bonus (PvE Only)
		// 17 - Endurance
		// 53 - Evade (PvE Only)
		// 31 - Fatigue Cap
		// 6 - Focus
		// 16 - Heal Bonus
		// 40 - Health Regeneration
		// 4 - Hits
		// 29 - Hits Cap
		// 8 - Mellee Damage
		// 19 - Mellee Haste
		// 62 - Mythical Block
		// 63 - Mythical Coin
		// 66 - Mythical Crowd Cont
		// 71 - Mythical DPS
		// 55 - Mythical Encumbrance
		// 78 - Mythical Endurance Regen
		// 67 - Mythical Essence Re
		// 61 - Mythical Evade
		// 76 - Mythical Health Regeneration
		// 60 - Mythical Parry
		// 80 - Mythical Physical Defense
		// 77 - Mythical Power Regen
		// 72 - Mythical Realm Points
		// 57 - Mythical Resist Cap
		// 68 - Mythical Resist and ??
		// 74 - Mythical Ressurection 
		// 70 - Mythical Run Speed
		// 79 - Mythical Safe Fall
		// 69 - Mythical Siege Damage
		// 58 - Mythical Siege Speed
		// 73 - Mythical Spell Increase
		// 64 - Mythical Stat Cap
		// 75 - Mythical Stat and Ca ??
		// 65 - Mythical Water Breathing
		// 56 - Mythical Water Movement
		// 46 - Negative Effect Duration
		// 51 - Parry (PvE Only)
		// 42 - Piece Ablative (PvE Only)
		// 3 - Power
		// 34 - Power %
		// 30 - Power Cap
		// 41 - Power Regeneration
		// 54 - Reactionary Style Damage
		// 5 - Resist
		// 2 - Skill
		// 9 - Spell Damage
		// 13 - Spell Duration
		// 21 - Spell Haste
		// 32 - Spell Piercing
		// 37 - Spell Power Cost Regeneration
		// 12 - Spell Range
		// 1 - Stat
		// 28 - Stats Cap
		// 47 - Style Cost Reduction
		// 10 - Style damage
		// 48 - To-Hit Bonus (PvE Only)


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Artifact 
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 1 - Arcane Siphon
		// 7 - Bonus BP
		// 5 - Bonus Gold
		// 6 - Bonus RP
		// 4 - Bonus XP
		// 2 - Conversion
		// 3 - Radiant Aura


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Craft Skill Gain 
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 4 - Alchemy
		// 2 - Armorcrafting
		// 12 - Fletching
		// 13 - Spellcraft
		// 11 - Tailoring
		// 1 - Weaponcrafting


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Craft Speed 
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 4 - Alchemy
		// 2 - Armorcrafting
		// 12 - Fletching
		// 13 - Spellcraft
		// 11 - Tailoring
		// 1 - Weaponcrafting


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Focus (Albion)
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 304 - All Spell Lines
		// 72 - Body Magic
		// 68 - Cold Magic
		// 122 - Death Servant
		// 120 - Deathsight
		// 69 - Earth Magic
		// 66 - Fire Magic
		// 71 - Matter Magic
		// 74 - Mind Magic
		// 121 - Painworking
		// 73 - Spirit Magic
		// 67 - Wind Magic

		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Mythical Resist Cap
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 16 - Body
		// 12 - Cold
		// 1 - Crush
		// 22 - Energy
		// 10 - Heat
		// 15 - Matter
		// 2 - Slash
		// 17 - Spirit
		// 3 - Thrust


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Mythical Resist And ???
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 16 - Body
		// 12 - Cold
		// 1 - Crush
		// 22 - Energy
		// 10 - Heat
		// 15 - Matter
		// 2 - Slash
		// 17 - Spirit
		// 3 - Thrust



		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Mythical Stat Cap
		// ------------------------------------------------------------------------- 
		// 65535 - Any
		// 15 - All Stats
		// 10 - Acuity
		// 7 - Charisma
		// 2 - Constitution
		// 64 - Dexterity
		// 6 - Empathy
		// 4 - Intelligence
		// 5 - Piety
		// 3 - Quickness
		// 0 - Strength


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Mythical Stat And Ca???
		// ------------------------------------------------------------------------- 
		// 65535 - Any
		// 15 - All Stats
		// 10 - Acuity
		// 7 - Charisma
		// 2 - Constitution
		// 64 - Dexterity
		// 6 - Empathy
		// 4 - Intelligence
		// 5 - Piety
		// 3 - Quickness
		// 0 - Strength


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Resist
		// ------------------------------------------------------------------------- 
		// 0 - Any
		// 16 - Body
		// 12 - Cold
		// 1 - Crush
		// 22 - Energy
		// 10 - Heat
		// 15 - Matter
		// 2 - Slash
		// 17 - Spirit
		// 3 - Thrust


		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Bonuses Values - Skill (Albion)
		// -------------------------------------------------------------------------
		// 0 - Any
		// 302 - All Archery
		// 303 - All Casting
		// 301 - All Dual wield
		// 300 - All Primary Melee
		// 146 - Aura manipulation
		// 72 - Body Magic
		// 68 - Cold Magic
		// 118 - Critical Strinke
		// 91 - Crossbow
		// 33 - Crush
		// 122 - Death Servant
		// 120 - Deathsight
		// 77 - Dual wield
		// 69 - Earth Magic
		// 83 - Enhancements
		// 117 - Envenom
		// 66 - Fire Magic
		// 147 - Fist Wraps
		// 46 - Flex Weapons
		// 98 - Instruments
		// 145 - Magnetism
		// 71 - Matter Magic
		// 148 - Mauler Staff
		// 74 - Mind Magic
		// 121 - Painworking
		// 8 - Parry
		// 64 - Polearm
		// 144 - Power Strikes
		// 88 - Rejuvenation
		// 43 - Shield
		// 1 - Slash
		// 89 - Smiting
		// 123 - Soulrending
		// 73 - Spirit Magic
		// 47 - Staff
		// 19 - Stealth
		// 2 - Thrust
		// 65 - Two Handed
		// 67 - Wind Magic


		*/
		#endregion


	}

}
