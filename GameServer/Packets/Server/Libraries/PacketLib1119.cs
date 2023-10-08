﻿using System.Collections.Generic;
using DOL.Database;

namespace DOL.GS.PacketHandler
{
	[PacketLib(1119, GameClient.eClientVersion.Version1119)]
	public class PacketLib1119 : PacketLib1118
	{
		/// <summary>
		/// Constructs a new PacketLib for Client Version 1.119
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib1119(GameClient client)
			: base(client)
		{
		}

		/// <summary>
		/// New item data packet for 1.119
		/// </summary>
		protected override void WriteItemData(GsTcpPacketOut pak, DbInventoryItem item)
		{
			if (item == null)
			{
				pak.Fill(0x00, 24); // +1 byte: item.Effect changed to short
				return;
			}
			pak.WriteShort((ushort)0); // item uniqueID
			pak.WriteByte((byte)item.Level);

			int value1; // some object types use this field to display count
			int value2; // some object types use this field to display count
			switch (item.Object_Type)
			{
				case (int)EObjectType.GenericItem:
					value1 = item.Count & 0xFF;
					value2 = (item.Count >> 8) & 0xFF;
					break;
				case (int)EObjectType.Arrow:
				case (int)EObjectType.Bolt:
				case (int)EObjectType.Poison:
					value1 = item.Count;
					value2 = item.SPD_ABS;
					break;
				case (int)EObjectType.Thrown:
					value1 = item.DPS_AF;
					value2 = item.Count;
					break;
				case (int)EObjectType.Instrument:
					value1 = (item.DPS_AF == 2 ? 0 : item.DPS_AF);
					value2 = 0;
					break; // unused
				case (int)EObjectType.Shield:
					value1 = item.Type_Damage;
					value2 = item.DPS_AF;
					break;
				case (int)EObjectType.AlchemyTincture:
				case (int)EObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)EObjectType.HouseWallObject:
				case (int)EObjectType.HouseFloorObject:
				case (int)EObjectType.GardenObject:
					value1 = 0;
					value2 = item.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = item.DPS_AF;
					value2 = item.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (item.Object_Type == (int)EObjectType.GardenObject)
				pak.WriteByte((byte)(item.DPS_AF));
			else
				pak.WriteByte((byte)(item.Hand << 6));

			pak.WriteByte((byte)((item.Type_Damage > 3 ? 0 : item.Type_Damage << 6) | item.Object_Type));
			pak.WriteByte(0x00); //unk 1.112
			pak.WriteShort((ushort)item.Weight);
			pak.WriteByte(item.ConditionPercent); // % of con
			pak.WriteByte(item.DurabilityPercent); // % of dur
			pak.WriteByte((byte)item.Quality); // % of qua
			pak.WriteByte((byte)item.Bonus); // % bonus
			pak.WriteByte((byte)item.BonusLevel); // 1.109
			pak.WriteShort((ushort)item.Model);
			pak.WriteByte((byte)item.Extension);
			int flag = 0;
			int emblem = item.Emblem;
			int color = item.Color;
			if (emblem != 0)
			{
				pak.WriteShort((ushort)emblem);
				flag |= (emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
			}
			else
			{
				pak.WriteShort((ushort)color);
			}
			//						flag |= 0x01; // newGuildEmblem
			flag |= 0x02; // enable salvage button

			// Enable craft button if the item can be modified and the player has alchemy or spellcrafting
			ECraftingSkill skill = CraftingMgr.GetCraftingSkill(item);
			switch (skill)
			{
				case ECraftingSkill.ArmorCrafting:
				case ECraftingSkill.Fletching:
				case ECraftingSkill.Tailoring:
				case ECraftingSkill.WeaponCrafting:
					if (m_gameClient.Player.CraftingSkills.ContainsKey(ECraftingSkill.Alchemy)
						|| m_gameClient.Player.CraftingSkills.ContainsKey(ECraftingSkill.SpellCrafting))
						flag |= 0x04; // enable craft button
					break;

				default:
					break;
			}

			ushort icon1 = 0;
			ushort icon2 = 0;
			string spell_name1 = "";
			string spell_name2 = "";
			if (item.Object_Type != (int)EObjectType.AlchemyTincture)
			{
				if (item.SpellID > 0/* && item.Charges > 0*/)
				{
					SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
					if (chargeEffectsLine != null)
					{
						List<Spell> spells = SkillBase.GetSpellList(chargeEffectsLine.KeyName);
						foreach (Spell spl in spells)
						{
							if (spl.ID == item.SpellID)
							{
								flag |= 0x08;
								icon1 = spl.Icon;
								spell_name1 = spl.Name; // or best spl.Name ?
								break;
							}
						}
					}
				}
				if (item.SpellID1 > 0/* && item.Charges > 0*/)
				{
					SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);
					if (chargeEffectsLine != null)
					{
						List<Spell> spells = SkillBase.GetSpellList(chargeEffectsLine.KeyName);
						foreach (Spell spl in spells)
						{
							if (spl.ID == item.SpellID1)
							{
								flag |= 0x10;
								icon2 = spl.Icon;
								spell_name2 = spl.Name; // or best spl.Name ?
								break;
							}
						}
					}
				}
			}
			pak.WriteByte((byte)flag);
			if ((flag & 0x08) == 0x08)
			{
				pak.WriteShort((ushort)icon1);
				pak.WritePascalString(spell_name1);
			}
			if ((flag & 0x10) == 0x10)
			{
				pak.WriteShort((ushort)icon2);
				pak.WritePascalString(spell_name2);
			}
			pak.WriteShort((ushort)item.Effect); // item effect changed to short
			string name = item.Name;
			if (item.Count > 1)
				name = item.Count + " " + name;
			if (item.SellPrice > 0)
			{
				if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
					name += "[" + item.SellPrice.ToString() + " BP]";
				else
					name += "[" + MoneyMgr.GetString(item.SellPrice) + "]";
			}
			if (name == null) name = "";
			if (name.Length > 55)
				name = name.Substring(0, 55);
			pak.WritePascalString(name);
		}
	}
}
