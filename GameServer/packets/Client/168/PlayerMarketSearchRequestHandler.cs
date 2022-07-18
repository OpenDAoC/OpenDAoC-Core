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
using System.Collections;
using System.Collections.Generic;
using DOL.Database;
using DOL.GS.Housing;
using System.Text;
using log4net;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.MarketSearchRequest, "Handles player market search", eClientStatus.PlayerInGame)]
	public class PlayerMarketSearchRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			if (client == null || client.Player == null)
				return;

			if ((client.Player.TargetObject is IGameInventoryObject) == false)
				return;

			var searchOffset = packet.ReadByte();
			packet.Skip(3); // 4 bytes unused

			MarketSearch.SearchData search = new MarketSearch.SearchData();
			//{
			//	name = packet.ReadString(64),
			//	packet.Skip(1),
			//	slot = (int)packet.ReadByte(),
			//	skill = (int)packet.ReadInt(),
			//	resist = (int)packet.ReadInt(),
			//	bonus = (int)packet.ReadInt(),
			//	hp = (int)packet.ReadInt(),
			//	power = (int)packet.ReadInt(),
			//	proc = (int)packet.ReadInt(),
			//	qtyMin = (int)packet.ReadInt(),
			//	qtyMax = (int)packet.ReadInt(),
			//	levelMin = (int)packet.ReadInt(),
			//	levelMax = (int)packet.ReadInt(),
			//	priceMin = (int)packet.ReadInt(),
			//	priceMax = (int)packet.ReadInt(),
			//	visual = (int)packet.ReadInt(),
			//	page = (byte)packet.ReadByte()
			//};

			search.name = packet.ReadString(searchOffset);
			// found weird searches after searching for a string, so we are forcing the weirdness
			if (search.name.Equals("|"))
			{
				search.name = "";
			}

			//packet.Skip(1);
			search.slot = (int)packet.ReadByte();
			//search.skill = (int)packet.ReadInt();
			//search.resist = (int)packet.ReadInt();
			//search.bonus = (int)packet.ReadInt();
			var bonus1 = packet.ReadByte();
			var bonus1b = packet.ReadByte();
			search.bonus1 = bonus1b * 256 + bonus1;

			var bonus1Value = (int)packet.ReadByte();
			var bonus1bValue = (int)packet.ReadByte();
			search.bonus1Value = bonus1bValue * 256 + bonus1Value;

			var bonus2 = (short)packet.ReadByte();
			var bonus2b = packet.ReadByte();
			search.bonus2 = bonus2b * 256 + bonus2;

			var bonus2Value = (short)packet.ReadByte();
			var bonus2bValue = (int)packet.ReadByte();
			search.bonus2Value = bonus2bValue * 256 + bonus2Value;

			var bonus3 = (short)packet.ReadByte();
			var bonus3b = packet.ReadByte();
			search.bonus3 = bonus3b * 256 + bonus3;

			var bonus3Value = (short)packet.ReadByte();
			var bonus3bValue = (int)packet.ReadByte();
			search.bonus3Value = bonus3bValue * 256 + bonus3Value;

			search.proc = (int)packet.ReadByte();
			packet.Skip(1);
			//short unk2 = (short)packet.ReadShort();
			search.armorType = (byte)packet.ReadByte();
			search.damageType = (byte)packet.ReadByte(); // 1=crush, 2=slash, 3=thrust
			search.levelMin = (byte)packet.ReadByte();
			search.levelMax = (byte)packet.ReadByte();
			search.minQual = (byte)packet.ReadByte();

			var priceMin1 = packet.ReadByte();
			var priceMin1b = packet.ReadByte();
			priceMin1b = priceMin1b != 0 ? priceMin1b : 1;
			var priceMin1c = packet.ReadByte();
			priceMin1c = priceMin1c != 0 ? priceMin1c : 1;
			var priceMin1d = packet.ReadByte();
			priceMin1d = priceMin1d != 0 ? priceMin1d : 1;
			priceMin1d = priceMin1b == 1 && priceMin1c == 1 && priceMin1d == 1 ? 0 : priceMin1d;
			search.priceMin = (uint)(priceMin1b * priceMin1c * priceMin1d * 256 + priceMin1);

			var priceMax1 = packet.ReadByte();
			var priceMax1b = packet.ReadByte();
			priceMax1b = priceMax1b != 0 ? priceMax1b : 1;
			var priceMax1c = packet.ReadByte();
			priceMax1c = priceMax1c != 0 ? priceMax1c : 1;
			var priceMax1d = packet.ReadByte();
			priceMax1d = priceMax1d != 0 ? priceMax1d : 1;
			priceMax1d = priceMax1b == 1 && priceMax1c == 1 && priceMax1d == 1 ? 0 : priceMax1d;
			search.priceMax = (uint)(priceMax1b * priceMax1c * priceMax1d * 256 + priceMax1);


			search.playerCrafted = (byte)packet.ReadByte(); // 1 = show only Player crafted, 0 = all
			search.visual = (int)packet.ReadByte();
			search.page = (byte)packet.ReadByte();


			//search.hp = (int)packet.ReadInt();
			//search.power = (int)packet.ReadInt();

			//search.qtyMax = (int)packet.ReadInt();


			//byte unk1 = (byte)packet.ReadByte();
			////short unk2 = (short)packet.ReadShort();

			//// Dunnerholl 2009-07-28 Version 1.98 introduced new options to Market search. 12 Bytes were added, but only 7 are in usage so far in my findings.
			//// update this, when packets change and keep in mind, that this code reflects only the 1.98 changes
			//search.armorType = search.page; // page is now used for the armorType (still has to be logged, i just checked that 2 means leather, 0 = standard

			//byte unk3 = (byte)packet.ReadByte();
			//byte unk4 = (byte)packet.ReadByte();
			//byte unk5 = (byte)packet.ReadByte();

			////packet.Skip(3); // 3 bytes unused
			//search.page = (byte)packet.ReadByte(); // page is now sent here
			//byte unk6 = (byte)packet.ReadByte();
			//byte unk7 = (byte)packet.ReadByte();
			//byte unk8 = (byte)packet.ReadByte();

			search.clientVersion = client.Version.ToString();


			Console.WriteLine(search);

			if (ServerProperties.Properties.MARKET_ENABLE_LOG)
			{
			
				log.DebugFormat(" ");
				log.DebugFormat("----- MARKET EXPLORER SEARCH PACKET ANALYSIS ---------------------");
				log.DebugFormat(" ");
				log.DebugFormat("name          : {0}", search.name.ToString());
				log.DebugFormat("slot          : {0}", GetPacketDescriptionSlot(search.slot));
				log.DebugFormat("bonus1        : {0}", GetPacketDescriptionBonuses(search.bonus1));
				log.DebugFormat("bonus1Value   : {0}", GetPacketDescriptionBonusesValues(search.bonus1, search.bonus1Value));
				log.DebugFormat("bonus2        : {0}", GetPacketDescriptionBonuses(search.bonus2));
				log.DebugFormat("bonus2Value   : {0}", GetPacketDescriptionBonusesValues(search.bonus2, search.bonus2Value));
				log.DebugFormat("bonus3        : {0}", GetPacketDescriptionBonuses(search.bonus3));
				log.DebugFormat("bonus3Value   : {0}", GetPacketDescriptionBonusesValues(search.bonus3, search.bonus3Value));
				log.DebugFormat("item ability  : {0}", GetPacketDescriptionItemAbilities(search.proc));
				log.DebugFormat("armorType     : {0}", GetPacketDescriptionArmorType(search.armorType));
				log.DebugFormat("damageType    : {0}", GetPacketDescriptionDamageType(search.damageType));
				log.DebugFormat("levelMin      : {0}", search.levelMin.ToString());
				log.DebugFormat("levelMax      : {0}", search.levelMax.ToString());
				log.DebugFormat("minQual       : {0}", search.minQual.ToString());
				log.DebugFormat("priceMin      : {0}", search.priceMin.ToString());
				log.DebugFormat("priceMax      : {0}", search.priceMax.ToString());
				log.DebugFormat("playerCrafted : {0}", search.playerCrafted.ToString());
				log.DebugFormat("visual        : {0}", search.visual.ToString());
				log.DebugFormat("page          : {0}", search.page.ToString());
				log.DebugFormat("clientVersion : {0}", search.clientVersion.ToString());
				log.DebugFormat(" ");
				log.DebugFormat("------------------------------------------------------------------");
				log.DebugFormat(" ");
			}


			// -------------------------------------------------------------------
			// search result output
			(client.Player.TargetObject as IGameInventoryObject).SearchInventory(client.Player, search);
		}





		// --------------------------------------------------------------------------
		// Private functions to handle conversion for debug purposes
		// --------------------------------------------------------------------------


		#region ALL Packet Enumerations

		/*

		// -------------------------------------------------------------------------
		// Market Explorer Packets Debug - Slot (slot)
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
		// Market Explorer Packets Debug - Item Abilities (proc)
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
		// 41 - Chained Direct Damage
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
		// Market Explorer Packets Debug - Armor Type  
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



		#region Packet Description - SLOT
		public string GetPacketDescriptionSlot(int slotID)
		{
			string packetDescription = "";

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

			switch (slotID)
			{
				case 0:
					packetDescription = "0 -Any";
					break;
				case 1:
					packetDescription = "1 - Head";
					break;
				case 2:
					packetDescription = "2 - Hands";
					break;
				case 3:
					packetDescription = "3 - Feet";
					break;
				case 4:
					packetDescription = "4 - Jewel";
					break;
				case 5:
					packetDescription = "5 - Torso";
					break;
				case 6:
					packetDescription = "6 - Cloak";
					break;
				case 7:
					packetDescription = "7 - Legs";
					break;
				case 8:
					packetDescription = "8 - Arms";
					break;
				case 9:
					packetDescription = "9 - Neck";
					break;
				case 12:
					packetDescription = "12 - Waist";
					break;
				case 13:
					packetDescription = "13 - Wrist";
					break;
				case 15:
					packetDescription = "15 - Ring";
					break;
				case 17:
					packetDescription = "17 - Mythical";
					break;
				case 100:
					packetDescription = "100 - One Handed";
					break;
				case 101:
					packetDescription = "101 - Left Hand";
					break;
				case 102:
					packetDescription = "102 - Two Handed";
					break;
				case 103:
					packetDescription = "103 - Ranged";
					break;
				case 104:
					packetDescription = "104 - Instrument";
					break;
				case 105:
					packetDescription = "105 - Shield";
					break;
				case 106:
					packetDescription = "106 - Wieldable";
					break;
				default:
					packetDescription = "Slot ID not defined";
					break;
			}

			return packetDescription;
		}
		#endregion



		#region Packet Description - Item Abilities
		public string GetPacketDescriptionItemAbilities(int itemAbilityID)
		{
			string packetDescription = "";

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
			// 41 - Chained Direct Damage
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

			switch (itemAbilityID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 17: packetDescription = "17 - Accuracy Boost"; break;
				case 2: packetDescription = "2 - Add Spell Radius"; break;
				case 9: packetDescription = "9 - Arcane Leadership"; break;
				case 5: packetDescription = "5 - Arch Magery"; break;
				case 113: packetDescription = "113 - Armor Factor Buff"; break;
				case 88: packetDescription = "88 - Armor Factor Debuff"; break;
				case 6: packetDescription = "6 - Armor Whiter"; break;
				case 80: packetDescription = "80 - Arrogance"; break;
				case 7: packetDescription = "7 - Arrogance (Invocation)"; break;
				case 18: packetDescription = "18 - Attack Speed Decrease"; break;
				case 10: packetDescription = "10 - Aura of Kings"; break;
				case 12: packetDescription = "12 - Bedazzled"; break;
				case 19: packetDescription = "19 - Bladeturn"; break;
				case 20: packetDescription = "20 - Block/Parry Boost"; break;
				case 13: packetDescription = "13 - Bolt"; break;
				case 3: packetDescription = "3 - Boon of Kings"; break;
				case 15: packetDescription = "15 - Buff Shear"; break;
				case 41: packetDescription = "41 - Chained Direct Damage"; break;
				case 11: packetDescription = "11 - Cheat Death"; break;
				case 30: packetDescription = "30 - Comprehension Boost"; break;
				case 110: packetDescription = "110 - Cooldown reset"; break;
				case 31: packetDescription = "31 - Cost Reduction"; break;
				case 74: packetDescription = "74 - Create Item"; break;
				case 109: packetDescription = "109 - Cure Mesmerize"; break;
				case 44: packetDescription = "44 - Damage Add"; break;
				case 38: packetDescription = "38 - Damage Conversion"; break;
				case 45: packetDescription = "45 - Damage Reduction"; break;
				case 47: packetDescription = "47 - Damage Shield"; break;
				case 48: packetDescription = "48 - Damage to Power"; break;
				case 64: packetDescription = "64 - Decrease Casting Speed"; break;
				case 39: packetDescription = "39 - Defensive Proc"; break;
				case 40: packetDescription = "40 - Direct Damage"; break;
				case 42: packetDescription = "42 - Disarmed"; break;
				case 43: packetDescription = "43 - Disease"; break;
				case 82: packetDescription = "82 - Disoriented"; break;
				case 86: packetDescription = "86 - Dmg W/Resist Decrease"; break;
				case 49: packetDescription = "49 - Damage Over Time"; break;
				case 81: packetDescription = "81 - Effectiveness Debuff"; break;
				case 53: packetDescription = "53 - Efficient Endurance"; break;
				case 54: packetDescription = "54 - Efficient Healing"; break;
				case 55: packetDescription = "55 - Enchanter Pet Boost"; break;
				case 56: packetDescription = "56 - Endurance Drain"; break;
				case 61: packetDescription = "61 - Endurance Heal"; break;
				case 58: packetDescription = "58 - Endurance Regen"; break;
				case 21: packetDescription = "21 - Evade Boost"; break;
				case 22: packetDescription = "22 - Haste"; break;
				case 66: packetDescription = "66 - Heal"; break;
				case 107: packetDescription = "107 - Heal Over Time"; break;
				case 67: packetDescription = "67 - Heal Bonus"; break;
				case 106: packetDescription = "106 - Heal Hits Over Time"; break;
				case 59: packetDescription = "59 - Health Regen"; break;
				case 23: packetDescription = "23 - Ignore Bladeturn"; break;
				case 72: packetDescription = "72 - Illusion"; break;
				case 94: packetDescription = "94 - Improved Stat Decrease"; break;
				case 122: packetDescription = "122 - Improved Stat Enhancements"; break;
				case 65: packetDescription = "65 - Increase Attack Speed"; break;
				case 33: packetDescription = "33 - Increase Fumble Chance"; break;
				case 35: packetDescription = "35 - Increase Mellee Critical"; break;
				case 36: packetDescription = "36 - Increase Spell Critical"; break;
				case 75: packetDescription = "75 - Level effectiveness"; break;
				case 76: packetDescription = "76 - Lifedrain"; break;
				case 79: packetDescription = "79 - Lore Debuff"; break;
				case 70: packetDescription = "70 - Magic Health Buffer"; break;
				case 1: packetDescription = "1 - Mellee Absorption"; break;
				case 78: packetDescription = "78 - Mellee Absorsion Debuff"; break;
				case 37: packetDescription = "37 - Mellee Damage Boost"; break;
				case 24: packetDescription = "24 - Mesmerization Feed"; break;
				case 25: packetDescription = "25 - Mesmerize"; break;
				case 77: packetDescription = "77 - Mesmerize Duration"; break;
				case 96: packetDescription = "96 - Offensive Proc"; break;
				case 98: packetDescription = "98 - Omni-Heal"; break;
				case 97: packetDescription = "97 - Omni-Lifedrain"; break;
				case 26: packetDescription = "26 - Parry"; break;
				case 100: packetDescription = "100 - Pet Cast"; break;
				case 32: packetDescription = "32 - Pet Scare"; break;
				case 71: packetDescription = "71 - Physical Health Buffer"; break;
				case 60: packetDescription = "60 - Power Regen"; break;
				case 102: packetDescription = "102 - Power Transfer"; break;
				case 103: packetDescription = "103 - Powershield"; break;
				case 4: packetDescription = "4 - Raise Dead"; break;
				case 90: packetDescription = "90 - Rampage"; break;
				case 104: packetDescription = "104 - Realm Lore"; break;
				case 105: packetDescription = "105 - Recovery"; break;
				case 108: packetDescription = "108 - Remove Negative Effect"; break;
				case 101: packetDescription = "101 - Replenish Power"; break;
				case 111: packetDescription = "111 - Reset Quickcast"; break;
				case 27: packetDescription = "27 - Resistence Bonus"; break;
				case 87: packetDescription = "87 - Resistence Decrease"; break;
				case 112: packetDescription = "112 - Resistance Enhance"; break;
				case 123: packetDescription = "123 - Reward Bonus"; break;
				case 85: packetDescription = "85 - Shatter illusion"; break;
				case 46: packetDescription = "46 - Shield/Damage Return"; break;
				case 114: packetDescription = "114 - Siege Lore"; break;
				case 116: packetDescription = "116 - Speed Decrease"; break;
				case 117: packetDescription = "117 - Speed Decrease W/Debuff"; break;
				case 28: packetDescription = "28 - Speed Enhancement"; break;
				case 118: packetDescription = "118 - Spell Pulse"; break;
				case 119: packetDescription = "119 - Spreadheal"; break;
				case 89: packetDescription = "89 - Stat Decrease"; break;
				case 92: packetDescription = "92 - Stat Drain"; break;
				case 120: packetDescription = "120 - Stat Enhancement"; break;
				case 73: packetDescription = "73 - Stealth"; break;
				case 84: packetDescription = "84 - Stealth Lore"; break;
				case 99: packetDescription = "99 - Stun"; break;
				case 29: packetDescription = "29 - Stun Feedback"; break;
				case 93: packetDescription = "93 - Style Damage Shield"; break;
				case 50: packetDescription = "50 - Summon"; break;
				case 51: packetDescription = "51 - Summon Elemental"; break;
				case 52: packetDescription = "52 - Summon Turret"; break;
				case 121: packetDescription = "121 - Tempest"; break;
				case 91: packetDescription = "91 - Vision of Malice"; break;
				case 14: packetDescription = "14 - Water Breathing"; break;
				case 68: packetDescription = "68 - Wave of Healing"; break;
				case 115: packetDescription = "115 - Weaponskill Buff"; break;
				case 95: packetDescription = "95 - Weaponskill Debuff"; break;
				case 62: packetDescription = "62 - Weight of a Feather"; break;


				default:
					break;
			}

			return packetDescription;

		}
		#endregion



		#region Packet Description - Damage Type
		public string GetPacketDescriptionDamageType(int damageTypeID)
		{
			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Damage Type 
			// -------------------------------------------------------------------------
			// 0 - Any
			// 1 - Crush
			// 2 - Slash
			// 3 - Thrust

			string packetDescription = "";

			switch (damageTypeID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 1: packetDescription = "1 - Crush"; break;
				case 2: packetDescription = "2 - Slash"; break;
				case 3: packetDescription = "3 - Thrust"; break;
				default: packetDescription = damageTypeID + " - Damage Type NOT DEFINED"; break;
			}
			return packetDescription;
		}

		#endregion



		#region Packet Description - Armor Type
		public string GetPacketDescriptionArmorType(int armorClassID)
		{
			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Armor Type  
			// -------------------------------------------------------------------------
			// 0 - Any
			// 1 - Cloth
			// 2 - Leather
			// 3 - Studded
			// 4 - Chain
			// 5 - Plate
			// 6 - Reinforced
			// 7 - Scale

			string packetDescription = "";

			switch (armorClassID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 1: packetDescription = "1 - Cloth"; break;
				case 2: packetDescription = "2 - Leather"; break;
				case 3: packetDescription = "3 - Studded"; break;
				case 4: packetDescription = "4 - Chain"; break;
				case 5: packetDescription = "5 - Plate"; break;
				case 6: packetDescription = "6 - Reinforced"; break;
				case 7: packetDescription = "7 - Scale"; break;

				default: packetDescription = armorClassID + " - Armor Class NOT DEFINED"; break;

			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses
		public string GetPacketDescriptionBonuses(int bonusID)
		{
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


			string packetDescription = "";

			switch (bonusID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 27: packetDescription = "27 - Archery Damage"; break;
				case 20: packetDescription = "20 - Archery Haste"; break;
				case 35: packetDescription = "35 - Artifact"; break;
				case 50: packetDescription = "50 - Bladeturn Reinforce"; break;
				case 52: packetDescription = "52 - Block (PvE Only)"; break;
				case 22: packetDescription = "22 - Bonus AF"; break;
				case 14: packetDescription = "14 - Buff Bonus"; break;
				case 38: packetDescription = "38 - Concentration"; break;
				case 26: packetDescription = "26 - Craft Skill Gain"; break;
				case 25: packetDescription = "25 - Craft Speed"; break;
				case 43: packetDescription = "43 - Damage Cap Reduction"; break;
				case 44: packetDescription = "44 - Death Experience Lost"; break;
				case 15: packetDescription = "15 - Debuff Bonus"; break;
				case 49: packetDescription = "49 - Defensive Bonus (PvE Only)"; break;
				case 17: packetDescription = "17 - Endurance"; break;
				case 53: packetDescription = "53 - Evade (PvE Only)"; break;
				case 31: packetDescription = "31 - Fatigue Cap"; break;
				case 6: packetDescription = "6 - Focus"; break;
				case 16: packetDescription = "16 - Heal Bonus"; break;
				case 40: packetDescription = "40 - Health Regeneration"; break;
				case 4: packetDescription = "4 - Hits"; break;
				case 29: packetDescription = "29 - Hits Cap"; break;
				case 8: packetDescription = "8 - Mellee Damage"; break;
				case 19: packetDescription = "19 - Mellee Haste"; break;
				case 62: packetDescription = "62 - Mythical Block"; break;
				case 63: packetDescription = "63 - Mythical Coin"; break;
				case 66: packetDescription = "66 - Mythical Crowd Cont"; break;
				case 71: packetDescription = "71 - Mythical DPS"; break;
				case 55: packetDescription = "55 - Mythical Encumbrance"; break;
				case 78: packetDescription = "78 - Mythical Endurance Regen"; break;
				case 67: packetDescription = "67 - Mythical Essence Re"; break;
				case 61: packetDescription = "61 - Mythical Evade"; break;
				case 76: packetDescription = "76 - Mythical Health Regeneration"; break;
				case 60: packetDescription = "60 - Mythical Parry"; break;
				case 80: packetDescription = "80 - Mythical Physical Defense"; break;
				case 77: packetDescription = "77 - Mythical Power Regen"; break;
				case 72: packetDescription = "72 - Mythical Realm Points"; break;
				case 57: packetDescription = "57 - Mythical Resist Cap"; break;
				case 68: packetDescription = "68 - Mythical Resist and ??"; break;
				case 74: packetDescription = "74 - Mythical Ressurection"; break;
				case 70: packetDescription = "70 - Mythical Run Speed"; break;
				case 79: packetDescription = "79 - Mythical Safe Fall"; break;
				case 69: packetDescription = "69 - Mythical Siege Damage"; break;
				case 58: packetDescription = "58 - Mythical Siege Speed"; break;
				case 73: packetDescription = "73 - Mythical Spell Increase"; break;
				case 64: packetDescription = "64 - Mythical Stat Cap"; break;
				case 75: packetDescription = "75 - Mythical Stat and Ca ??"; break;
				case 65: packetDescription = "65 - Mythical Water Breathing"; break;
				case 56: packetDescription = "56 - Mythical Water Movement"; break;
				case 46: packetDescription = "46 - Negative Effect Duration"; break;
				case 51: packetDescription = "51 - Parry (PvE Only)"; break;
				case 42: packetDescription = "42 - Piece Ablative (PvE Only)"; break;
				case 3: packetDescription = "3 - Power"; break;
				case 34: packetDescription = "34 - Power %"; break;
				case 30: packetDescription = "30 - Power Cap"; break;
				case 41: packetDescription = "41 - Power Regeneration"; break;
				case 54: packetDescription = "54 - Reactionary Style Damage"; break;
				case 5: packetDescription = "5 - Resist"; break;
				case 2: packetDescription = "2 - Skill"; break;
				case 9: packetDescription = "9 - Spell Damage"; break;
				case 13: packetDescription = "13 - Spell Duration"; break;
				case 21: packetDescription = "21 - Spell Haste"; break;
				case 32: packetDescription = "32 - Spell Piercing"; break;
				case 37: packetDescription = "37 - Spell Power Cost Regeneration"; break;
				case 12: packetDescription = "12 - Spell Range"; break;
				case 1: packetDescription = "1 - Stat"; break;
				case 28: packetDescription = "28 - Stats Cap"; break;
				case 47: packetDescription = "47 - Style Cost Reduction"; break;
				case 10: packetDescription = "10 - Style damage"; break;
				case 48: packetDescription = "48 - To-Hit Bonus (PvE Only)"; break;

				default: packetDescription = bonusID + " - Bonuses NOT DEFINED"; break;

			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values - Artifact
		public string GetPacketDescriptionBonusesValuesArtifact(int bonusValueID)
		{
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

			string packetDescription = "";

			switch (bonusValueID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 1: packetDescription = "1 - Arcane Siphon"; break;
				case 7: packetDescription = "7 - Bonus BP"; break;
				case 5: packetDescription = "5 - Bonus Gold"; break;
				case 6: packetDescription = "6 - Bonus RP"; break;
				case 4: packetDescription = "4 - Bonus XP"; break;
				case 2: packetDescription = "2 - Conversion"; break;
				case 3: packetDescription = "3 - Radiant Aura"; break;

				default: packetDescription = bonusValueID + " - Bonuses Values - Artifact - NOT DEFINED"; break;
			}

			return packetDescription;

		}

		#endregion



		#region Packet Description - Bonuses Values - Craft Skill
		public string GetPacketDescriptionBonusesValuesCraftSkill(int craftSkillID)
		{

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

			string packetDescription = "";

			switch (craftSkillID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 4: packetDescription = "4 - Alchemy"; break;
				case 2: packetDescription = "2 - Armorcrafting"; break;
				case 12: packetDescription = "12 - Fletching"; break;
				case 13: packetDescription = "13 - Spellcraft"; break;
				case 11: packetDescription = "11 - Tailoring"; break;
				case 1: packetDescription = "1 - Weaponcrafting"; break;
				default: packetDescription = craftSkillID + " - Bonuses Values - Craft Skill NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values - Craft Speed
		public string GetPacketDescriptionBonusesValuesCraftSpeed(int craftSpeedID)
		{

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


			string packetDescription = "";

			switch (craftSpeedID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 4: packetDescription = "4 - Alchemy"; break;
				case 2: packetDescription = "2 - Armorcrafting"; break;
				case 12: packetDescription = "12 - Fletching"; break;
				case 13: packetDescription = "13 - Spellcraft"; break;
				case 11: packetDescription = "11 - Tailoring"; break;
				case 1: packetDescription = "1 - Weaponcrafting"; break;
				default:
					packetDescription = craftSpeedID + " - Bonuses Values - Craft Speed NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values - Focus
		public string GetPacketDescriptionBonusesValuesFocus(int focusID)
		{
			string packetDescription = "";

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

			switch (focusID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 304: packetDescription = "304 - All Spell Lines"; break;
				case 72: packetDescription = "72 - Body Magic"; break;
				case 68: packetDescription = "68 - Cold Magic"; break;
				case 122: packetDescription = "122 - Death Servant"; break;
				case 120: packetDescription = "120 - Deathsight"; break;
				case 69: packetDescription = "69 - Earth Magic"; break;
				case 66: packetDescription = "66 - Fire Magic"; break;
				case 71: packetDescription = "71 - Matter Magic"; break;
				case 74: packetDescription = "74 - Mind Magic"; break;
				case 121: packetDescription = "121 - Painworking"; break;
				case 73: packetDescription = "73 - Spirit Magic"; break;
				case 67: packetDescription = "67 - Wind Magic"; break;
				default:
					packetDescription = focusID + " - Bonuses Values - Focus NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values - Mythical Resist Cap
		public string GetPacketDescriptionBonusesValuesMythicalResistCap(int resistID)
		{
			string packetDescription = "";

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

			switch (resistID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 16: packetDescription = "16 - Body"; break;
				case 12: packetDescription = "12 - Cold"; break;
				case 1: packetDescription = "1 - Crush"; break;
				case 22: packetDescription = "22 - Energy"; break;
				case 10: packetDescription = "10 - Heat"; break;
				case 15: packetDescription = "15 - Matter"; break;
				case 2: packetDescription = "2 - Slash"; break;
				case 17: packetDescription = "17 - Spirit"; break;
				case 3: packetDescription = "3 - Thrust"; break;

				default:
					packetDescription = resistID + " - Bonuses Values - Mythical Resist Cap NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values - Mythical Resist And
		public string GetPacketDescriptionBonusesValuesMythicalResistAnd(int resistID)
		{
			string packetDescription = "";

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Mythical Resist And
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

			switch (resistID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 16: packetDescription = "16 - Body"; break;
				case 12: packetDescription = "12 - Cold"; break;
				case 1: packetDescription = "1 - Crush"; break;
				case 22: packetDescription = "22 - Energy"; break;
				case 10: packetDescription = "10 - Heat"; break;
				case 15: packetDescription = "15 - Matter"; break;
				case 2: packetDescription = "2 - Slash"; break;
				case 17: packetDescription = "17 - Spirit"; break;
				case 3: packetDescription = "3 - Thrust"; break;
				default:
					packetDescription = resistID + " - Bonuses Values - Mythical Resist And NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Mythical Stat Cap
		public string GetPacketDescriptionBonusesValuesMythicalStatCap(int statID)
		{
			string packetDescription = "";

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

			switch (statID)
			{
				case 65535: packetDescription = "65535 - Any"; break;
				case 15: packetDescription = "15 - All Stats"; break;
				case 10: packetDescription = "10 - Acuity"; break;
				case 7: packetDescription = "7 - Charisma"; break;
				case 2: packetDescription = "2 - Constitution"; break;
				case 64: packetDescription = "64 - Dexterity"; break;
				case 6: packetDescription = "6 - Empathy"; break;
				case 4: packetDescription = "4 - Intelligence"; break;
				case 5: packetDescription = "5 - Piety"; break;
				case 3: packetDescription = "3 - Quickness"; break;
				case 0: packetDescription = "0 - Strength"; break;

				default:
					packetDescription = statID + " - Bonuses Values - Mythical Stat Cap NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Mythical Stat And Ca
		public string GetPacketDescriptionBonusesValuesMythicalStatAndCa(int statID)
		{
			string packetDescription = "";

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

			switch (statID)
			{
				case 65535: packetDescription = "65535 - Any"; break;
				case 15: packetDescription = "15 - All Stats"; break;
				case 10: packetDescription = "10 - Acuity"; break;
				case 7: packetDescription = "7 - Charisma"; break;
				case 2: packetDescription = "2 - Constitution"; break;
				case 64: packetDescription = "64 - Dexterity"; break;
				case 6: packetDescription = "6 - Empathy"; break;
				case 4: packetDescription = "4 - Intelligence"; break;
				case 5: packetDescription = "5 - Piety"; break;
				case 3: packetDescription = "3 - Quickness"; break;
				case 0: packetDescription = "0 - Strength"; break;
				default:
					packetDescription = statID + " - Bonuses Values - Mythical Stat And Ca NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Resist
		public string GetPacketDescriptionBonusesValuesResist(int resistID)
		{
			string packetDescription = "";

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

			switch (resistID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 16: packetDescription = "16 - Body"; break;
				case 12: packetDescription = "12 - Cold"; break;
				case 1: packetDescription = "1 - Crush"; break;
				case 22: packetDescription = "22 - Energy"; break;
				case 10: packetDescription = "10 - Heat"; break;
				case 15: packetDescription = "15 - Matter"; break;
				case 2: packetDescription = "2 - Slash"; break;
				case 17: packetDescription = "17 - Spirit"; break;
				case 3: packetDescription = "3 - Thrust"; break;
				default:
					packetDescription = resistID + " - Bonuses Values - Resist NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Skill
		public string GetPacketDescriptionBonusesValuesSkill(int skillID)
		{
			string packetDescription = "";

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

			switch (skillID)
			{
				case 0: packetDescription = "0 - Any"; break;
				case 302: packetDescription = "302 - All Archery"; break;
				case 303: packetDescription = "303 - All Casting"; break;
				case 301: packetDescription = "301 - All Dual wield"; break;
				case 300: packetDescription = "300 - All Primary Melee"; break;
				case 146: packetDescription = "146 - Aura manipulation"; break;
				case 72: packetDescription = "72 - Body Magic"; break;
				case 68: packetDescription = "68 - Cold Magic"; break;
				case 118: packetDescription = "118 - Critical Strinke"; break;
				case 91: packetDescription = "91 - Crossbow"; break;
				case 33: packetDescription = "33 - Crush"; break;
				case 122: packetDescription = "122 - Death Servant"; break;
				case 120: packetDescription = "120 - Deathsight"; break;
				case 77: packetDescription = "77 - Dual wield"; break;
				case 69: packetDescription = "69 - Earth Magic"; break;
				case 83: packetDescription = "83 - Enhancements"; break;
				case 117: packetDescription = "117 - Envenom"; break;
				case 66: packetDescription = "66 - Fire Magic"; break;
				case 147: packetDescription = "147 - Fist Wraps"; break;
				case 46: packetDescription = "46 - Flex Weapons"; break;
				case 98: packetDescription = "98 - Instruments"; break;
				case 145: packetDescription = "145 - Magnetism"; break;
				case 71: packetDescription = "71 - Matter Magic"; break;
				case 148: packetDescription = "148 - Mauler Staff"; break;
				case 74: packetDescription = "74 - Mind Magic"; break;
				case 121: packetDescription = "121 - Painworking"; break;
				case 8: packetDescription = "8 - Parry"; break;
				case 64: packetDescription = "64 - Polearm"; break;
				case 144: packetDescription = "144 - Power Strikes"; break;
				case 88: packetDescription = "88 - Rejuvenation"; break;
				case 43: packetDescription = "43 - Shield"; break;
				case 1: packetDescription = "1 - Slash"; break;
				case 89: packetDescription = "89 - Smiting"; break;
				case 123: packetDescription = "123 - Soulrending"; break;
				case 73: packetDescription = "73 - Spirit Magic"; break;
				case 47: packetDescription = "47 - Staff"; break;
				case 19: packetDescription = "19 - Stealth"; break;
				case 2: packetDescription = "2 - Thrust"; break;
				case 65: packetDescription = "65 - Two Handed"; break;
				case 67: packetDescription = "67 - Wind Magic"; break;
				default:
					packetDescription = skillID + " - Bonuses Values - Skill NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Stat
		public string GetPacketDescriptionBonusesValuesStat(int statID)
		{
			string packetDescription = "";

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Stat (Albion)
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


			switch (statID)
			{
				case 65535: packetDescription = "65535 - Any"; break;
				case 15: packetDescription = "15 - All Stats"; break;
				case 10: packetDescription = "10 - Acuity"; break;
				case 7: packetDescription = "7 - Charisma"; break;
				case 2: packetDescription = "2 - Constitution"; break;
				case 64: packetDescription = "64 - Dexterity"; break;
				case 6: packetDescription = "6 - Empathy"; break;
				case 4: packetDescription = "4 - Intelligence"; break;
				case 5: packetDescription = "5 - Piety"; break;
				case 3: packetDescription = "3 - Quickness"; break;
				case 0: packetDescription = "0 - Strength"; break;
				default:
					packetDescription = statID + " - Bonuses Values - Stat NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion




		#region Packet Description - Bonuses Values - Stats Cap
		public string GetPacketDescriptionBonusesValuesStatsCap(int statID)
		{
			string packetDescription = "";

			// -------------------------------------------------------------------------
			// Market Explorer Packets Debug - Bonuses Values - Stats Cap (Albion)
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


			switch (statID)
			{
				case 65535: packetDescription = "65535 - Any"; break;
				case 15: packetDescription = "15 - All Stats"; break;
				case 10: packetDescription = "10 - Acuity"; break;
				case 7: packetDescription = "7 - Charisma"; break;
				case 2: packetDescription = "2 - Constitution"; break;
				case 64: packetDescription = "64 - Dexterity"; break;
				case 6: packetDescription = "6 - Empathy"; break;
				case 4: packetDescription = "4 - Intelligence"; break;
				case 5: packetDescription = "5 - Piety"; break;
				case 3: packetDescription = "3 - Quickness"; break;
				case 0: packetDescription = "0 - Strength"; break;
				default:
					packetDescription = statID + " - Bonuses Values - Stat Cap NOT DEFINED"; break;
			}

			return packetDescription;
		}

		#endregion



		#region Packet Description - Bonuses Values (depending on bonus type)
		public string GetPacketDescriptionBonusesValues(int bonusID, int bonusValueID)
		{
			string packetDescription = "";

			// 35 - Artifact
			// 26 - Craft Skill Gain
			// 25 - Craft Speed
			// 6 - Focus
			// 57 - Mythical Resist Cap
			// 68 - Mythical Resist and ??
			// 64 - Mythical Stat Cap
			// 75 - Mythical Stat and Ca ??
			// 5 - Resist
			// 2 - Skill
			// 1 - Stat
			// 28 - Stats Cap


			switch (bonusID)
			{
				case 35: packetDescription = GetPacketDescriptionBonusesValuesArtifact(bonusValueID); break;
				case 26: packetDescription = GetPacketDescriptionBonusesValuesCraftSkill(bonusValueID); break;
				case 25: packetDescription = GetPacketDescriptionBonusesValuesCraftSpeed(bonusValueID); break;
				case 6: packetDescription = GetPacketDescriptionBonusesValuesFocus(bonusValueID); break;
				case 57: packetDescription = GetPacketDescriptionBonusesValuesMythicalResistCap(bonusValueID); break;
				case 68: packetDescription = GetPacketDescriptionBonusesValuesMythicalResistAnd(bonusValueID); break;
				case 64: packetDescription = GetPacketDescriptionBonusesValuesMythicalStatCap(bonusValueID); break;
				case 75: packetDescription = GetPacketDescriptionBonusesValuesMythicalStatAndCa(bonusValueID); break;
				case 5: packetDescription = GetPacketDescriptionBonusesValuesResist(bonusValueID); break;
				case 2: packetDescription = GetPacketDescriptionBonusesValuesSkill(bonusValueID); break;
				case 1: packetDescription = GetPacketDescriptionBonusesValuesStat(bonusValueID); break;
				case 28: packetDescription = GetPacketDescriptionBonusesValuesStatsCap(bonusValueID); break;

				default: packetDescription = bonusValueID.ToString(); break;

			}

			return packetDescription;
		}

		#endregion





	}
}