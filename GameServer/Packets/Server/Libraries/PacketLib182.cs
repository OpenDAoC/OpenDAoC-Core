using System.Collections.Generic;
using System.Reflection;
using Core.Database.Tables;
using Core.GS.Crafting;
using Core.GS.Enums;
using Core.GS.GameUtils;
using log4net;

namespace Core.GS.Packets.Server;

[PacketLib(182, GameClient.eClientVersion.Version182)]
public class PacketLib182 : PacketLib181
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	/// <summary>
	/// Constructs a new PacketLib for Version 1.82 clients
	/// </summary>
	/// <param name="client">the gameclient this lib is associated with</param>
	public PacketLib182(GameClient client)
		: base(client)
	{
	}

	protected override void SendInventorySlotsUpdateRange(ICollection<int> slots, EInventoryWindowType windowType)
	{
		using (GsTcpPacketOut pak = new GsTcpPacketOut(GetPacketCode(EServerPackets.InventoryUpdate)))
		{
			pak.WriteByte((byte)(slots == null ? 0 : slots.Count));
			pak.WriteByte((byte)((m_gameClient.Player.IsCloakHoodUp ? 0x01 : 0x00) | (int)m_gameClient.Player.rangeAttackComponent.ActiveQuiverSlot)); //bit0 is hood up bit4 to 7 is active quiver
			pak.WriteByte((byte)m_gameClient.Player.VisibleActiveWeaponSlots);
			pak.WriteByte((byte)windowType);
			if (slots != null)
			{
				foreach (int updatedSlot in slots)
				{
					if (updatedSlot >= (int)EInventorySlot.Consignment_First && updatedSlot <= (int)EInventorySlot.Consignment_Last)
						pak.WriteByte((byte)(updatedSlot - (int)EInventorySlot.Consignment_First + (int)EInventorySlot.HousingInventory_First));
					else
						pak.WriteByte((byte)(updatedSlot));

					DbInventoryItem item = null;
					item = m_gameClient.Player.Inventory.GetItem((EInventorySlot)updatedSlot);

					if (item == null)
					{
						pak.Fill(0x00, 19);
						continue;
					}
					pak.WriteByte((byte)item.Level);

					int value1; // some object types use this field to display count
					int value2; // some object types use this field to display count
					switch (item.Object_Type)
					{
						case (int)EObjectType.Arrow:
						case (int)EObjectType.Bolt:
						case (int)EObjectType.Poison:
						case (int)EObjectType.GenericItem:
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
					pak.WriteShort((ushort)item.Weight);
					pak.WriteByte(item.ConditionPercent); // % of con
					pak.WriteByte(item.DurabilityPercent); // % of dur
					pak.WriteByte((byte)item.Quality); // % of qua
					pak.WriteByte((byte)item.Bonus); // % bonus
					pak.WriteShort((ushort)item.Model);
					pak.WriteByte((byte)item.Extension);
					int flag = 0;
					if (item.Emblem != 0)
					{
						pak.WriteShort((ushort)item.Emblem);
						flag |= (item.Emblem & 0x010000) >> 16; // = 1 for newGuildEmblem
					}
					else
						pak.WriteShort((ushort)item.Color);
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
						SpellLine chargeEffectsLine = SkillBase.GetSpellLine(GlobalSpellsLines.Item_Effects);

						if (chargeEffectsLine != null)
						{
							if (item.SpellID > 0/* && item.Charges > 0*/)
							{
								Spell spell = SkillBase.FindSpell(item.SpellID, chargeEffectsLine);
								if (spell != null)
								{
									flag |= 0x08;
									icon1 = spell.Icon;
									spell_name1 = spell.Name; // or best spl.Name ?
								}
							}
							if (item.SpellID1 > 0/* && item.Charges > 0*/)
							{
								Spell spell = SkillBase.FindSpell(item.SpellID1, chargeEffectsLine);
								if (spell != null)
								{
									flag |= 0x10;
									icon2 = spell.Icon;
									spell_name2 = spell.Name; // or best spl.Name ?
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
					pak.WriteByte((byte)item.Effect);
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
					pak.WritePascalString(name);
				}
			}
			SendTCP(pak);
		}
	}
}