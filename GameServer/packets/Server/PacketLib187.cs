using System;
using System.Reflection;
using DOL.Database;
using DOL.GS.Quests;
using DOL.GS.Behaviour;
using System.Reflection.Metadata.Ecma335;
using System.Collections.Generic;

namespace DOL.GS.PacketHandler
{
	[PacketLib(187, GameClient.eClientVersion.Version187)]
	public class PacketLib187 : PacketLib186
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Constructs a new PacketLib for Version 1.87 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib187(GameClient client)
			: base(client)
		{
		}

		public override void SendQuestOfferWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, true);
		}

		public override void SendQuestRewardWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, false);
		}

		protected override void SendQuestWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest,	bool offer)
		{
			using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.Dialog)))
			{
				ushort QuestID = QuestMgr.GetIDForQuestType(quest.GetType());
				pak.WriteShort((offer) ? (byte)0x22 : (byte)0x21); // Dialog
				pak.WriteShort(QuestID);
				pak.WriteShort((ushort)questNPC.ObjectID);
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte(0x00); // unknown
				pak.WriteByte((offer) ? (byte)0x02 : (byte)0x01); // Accept/Decline or Finish/Not Yet
				pak.WriteByte(0x01); // Wrap
				pak.WritePascalString(quest.Name);

				if (quest.Summary.Length > 255)
				{
					pak.WritePascalString(quest.Summary.AsSpan(0, 255));
				}
				else
				{
					pak.WritePascalString(quest.Summary);
				}

				if (offer)
				{
					if (quest.Story.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteNonNullTerminatedString(quest.Story.AsSpan(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Story.Length);
						pak.WriteNonNullTerminatedString(quest.Story);
					}
				}
				else
				{
					if (quest.Conclusion.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteNonNullTerminatedString(quest.Conclusion.AsSpan(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Conclusion.Length);
						pak.WriteNonNullTerminatedString(quest.Conclusion);
					}
				}

				pak.WriteShort(QuestID);
				pak.WriteByte((byte)quest.Goals.Count); // #goals count
				foreach (RewardQuest.QuestGoal goal in quest.Goals)
				{
					pak.WritePascalString(String.Format("{0}\r", goal.Description));
				}
				pak.WriteByte((byte)quest.Level);
				pak.WriteByte((byte)quest.Rewards.MoneyPercent);
				pak.WriteByte((byte)quest.Rewards.ExperiencePercent(player));
				pak.WriteByte((byte)quest.Rewards.BasicItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.BasicItems)
					WriteTemplateData(pak, reward, 1);
				pak.WriteByte((byte)quest.Rewards.ChoiceOf);
				pak.WriteByte((byte)quest.Rewards.OptionalItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.OptionalItems)
					WriteTemplateData(pak, reward, 1);
				SendTCP(pak);
			}
		}

		public override void SendQuestOfferWindow(GameNPC questNPC, GamePlayer player, DQRewardQ quest) //patch 0026
		{
            SendQuestWindow(questNPC, player, quest, true);
        }

		public override void SendQuestRewardWindow(GameNPC questNPC, GamePlayer player, DQRewardQ quest) //patch 0026
		{
            SendQuestWindow(questNPC, player, quest, false);
        }

		protected virtual void SendQuestWindow(GameNPC questNPC, GamePlayer player, DQRewardQ quest, bool offer) // patch 0026
		{
            using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.Dialog)))
            {
                ushort QuestID = quest.ClientQuestID;
                pak.WriteShort((offer) ? (byte)0x22 : (byte)0x21); // Dialog
                pak.WriteShort(QuestID);
                pak.WriteShort((ushort)questNPC.ObjectID);
                pak.WriteByte(0x00); // unknown
                pak.WriteByte(0x00); // unknown
                pak.WriteByte(0x00); // unknown
                pak.WriteByte(0x00); // unknown
                pak.WriteByte((offer) ? (byte)0x02 : (byte)0x01); // Accept/Decline or Finish/Not Yet
                pak.WriteByte(0x01); // Wrap
                pak.WritePascalString($"{quest.Name} {quest.QuestLevel}");
				 

				if (quest.Description.Length > 255)
				{
					pak.WritePascalString(quest.Description.AsSpan(0, 255));
				}
				else
				{
					pak.WritePascalString(quest.Description);
				}

                if (offer)
				{
					if (quest.Story.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteNonNullTerminatedString(quest.Story.AsSpan(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Story.Length);
						pak.WriteNonNullTerminatedString(quest.Story);
					}
				}
				else
				{
					if (quest.FinishText.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteNonNullTerminatedString(quest.FinishText.AsSpan(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.FinishText.Length);
						pak.WriteNonNullTerminatedString(quest.FinishText);
					}
				}

                pak.WriteShort(QuestID);
                pak.WriteByte((byte)quest.Goals.Count); // #goals count
                foreach (DQRQuestGoal goal in quest.Goals)
                {
                    pak.WritePascalString(String.Format("{0}\r", goal.Description));
                }
                pak.WriteInt((uint)quest.RewardMoney);
                pak.WriteByte((byte)quest.ExperiencePercent(player));
                pak.WriteByte((byte)quest.FinalRewards.Count);
                int rewardLoc = 0;
                int optionalRewardLoc = 8;
                foreach (DbItemTemplate reward in quest.FinalRewards)
                {
					WriteItemData(pak, GameInventoryItem.Create(reward), (quest.ID * 16 + rewardLoc));
                    ++rewardLoc;
                }
                pak.WriteByte((byte)quest.NumOptionalRewardsChoice);
                pak.WriteByte((byte)quest.OptionalRewards.Count);
                foreach (DbItemTemplate reward in quest.OptionalRewards)
                {
					WriteItemData(pak, GameInventoryItem.Create(reward), (quest.ID * 16 + optionalRewardLoc));
					++optionalRewardLoc;
                }
                SendTCP(pak);
            }
        }

		/// <summary>
        /// patch 0020
        /// </summary>       
        protected virtual void WriteItemData(GSTCPPacketOut pak, DbInventoryItem item, int questID)
		{
			if (item == null)
			{
				pak.Fill(0x00, 24); //item.Effect changed to short 1.119
				return;
			}			
						
			pak.WriteShort((ushort)questID); // need to send an objectID for reward quest delve to work 1.115+
			pak.WriteByte((byte)item.Level);

			int value1; // some object types use this field to display count
			int value2; // some object types use this field to display count
			switch (item.Object_Type)
			{
				case (int)eObjectType.GenericItem:
					value1 = item.Count & 0xFF;
					value2 = (item.Count >> 8) & 0xFF;
					break;
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
					value1 = item.Count;
					value2 = item.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = item.DPS_AF;
					value2 = item.Count;
					break;
				case (int)eObjectType.Instrument:
					value1 = (item.DPS_AF == 2 ? 0 : item.DPS_AF);
					value2 = 0;
					break; // unused
				case (int)eObjectType.Shield:
					value1 = item.Type_Damage;
					value2 = item.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.HouseWallObject:
				case (int)eObjectType.HouseFloorObject:
				case (int)eObjectType.GardenObject:
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

			if (item.Object_Type == (int)eObjectType.GardenObject)
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
			//flag |= 0x01; // newGuildEmblem
			flag |= 0x02; // enable salvage button
			AbstractCraftingSkill skill = CraftingMgr.getSkillbyEnum(m_gameClient.Player.CraftingPrimarySkill);
			if (skill != null && skill is AdvancedCraftingSkill/* && ((AdvancedCraftingSkill)skill).IsAllowedToCombine(GameClient.Player, item)*/)
				flag |= 0x04; // enable craft button
			ushort icon1 = 0;
			ushort icon2 = 0;
			string spell_name1 = "";
			string spell_name2 = "";
			if (item.Object_Type != (int)eObjectType.AlchemyTincture)
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
			pak.WriteShort((ushort)item.Effect); // changed to short 1.119
			string name = item.Name;
			if (item.Count > 1)
				name = item.Count + " " + name;
			if (item.SellPrice > 0)
			{
				if (ServerProperties.Properties.CONSIGNMENT_USE_BP)
					name += "[" + item.SellPrice.ToString() + " BP]";
				else
					name += "[" + Money.GetString(item.SellPrice) + "]";
			}
			if (name == null) name = "";
			if (name.Length > 55)
				name = name.Substring(0, 55);
			pak.WritePascalString(name);
		}

		protected virtual void WriteTemplateData(GSTCPPacketOut pak, DbItemTemplate template, int count)
		{
			if (template == null)
			{
				pak.Fill(0x00, 19);
				return;
			}

			pak.WriteByte((byte)template.Level);

			int value1;
			int value2;

			switch (template.Object_Type)
			{
				case (int)eObjectType.Arrow:
				case (int)eObjectType.Bolt:
				case (int)eObjectType.Poison:
				case (int)eObjectType.GenericItem:
					value1 = count; // Count
					value2 = template.SPD_ABS;
					break;
				case (int)eObjectType.Thrown:
					value1 = template.DPS_AF;
					value2 = count; // Count
					break;
				case (int)eObjectType.Instrument:
					value1 = (template.DPS_AF == 2 ? 0 : template.DPS_AF);
					value2 = 0;
					break;
				case (int)eObjectType.Shield:
					value1 = template.Type_Damage;
					value2 = template.DPS_AF;
					break;
				case (int)eObjectType.AlchemyTincture:
				case (int)eObjectType.SpellcraftGem:
					value1 = 0;
					value2 = 0;
					/*
					must contain the quality of gem for spell craft and think same for tincture
					*/
					break;
				case (int)eObjectType.GardenObject:
					value1 = 0;
					value2 = template.SPD_ABS;
					/*
					Value2 byte sets the width, only lower 4 bits 'seem' to be used (so 1-15 only)

					The byte used for "Hand" (IE: Mini-delve showing a weapon as Left-Hand
					usabe/TwoHanded), the lower 4 bits store the height (1-15 only)
					*/
					break;

				default:
					value1 = template.DPS_AF;
					value2 = template.SPD_ABS;
					break;
			}
			pak.WriteByte((byte)value1);
			pak.WriteByte((byte)value2);

			if (template.Object_Type == (int)eObjectType.GardenObject)
				pak.WriteByte((byte)(template.DPS_AF));
			else
				pak.WriteByte((byte)(template.Hand << 6));
			pak.WriteByte((byte)((template.Type_Damage > 3
				? 0
				: template.Type_Damage << 6) | template.Object_Type));
			pak.WriteShort((ushort)template.Weight);
			pak.WriteByte(template.BaseConditionPercent);
			pak.WriteByte(template.BaseDurabilityPercent);
			pak.WriteByte((byte)template.Quality);
			pak.WriteByte((byte)template.Bonus);
			pak.WriteShort((ushort)template.Model);
			pak.WriteByte((byte)template.Extension);
			if (template.Emblem != 0)
				pak.WriteShort((ushort)template.Emblem);
			else
				pak.WriteShort((ushort)template.Color);
			pak.WriteByte((byte)0); // Flag
			pak.WriteByte((byte)template.Effect);
			if (count > 1)
				pak.WritePascalString(String.Format("{0} {1}", count, template.Name));
			else
				pak.WritePascalString(template.Name);
		}		

		protected override void SendQuestPacket(AbstractQuest quest, byte index)
		{
			if (quest is RewardQuest)
			{
				RewardQuest rewardQuest = quest as RewardQuest;
				using (var pak = PooledObjectFactory.GetForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.QuestEntry)))
				{
					pak.WriteByte(index);
					pak.WriteByte((byte)rewardQuest.Name.Length);
				pak.WriteShort(0x00); // unknown
				pak.WriteByte((byte)rewardQuest.Goals.Count);
				pak.WriteByte((byte)rewardQuest.Level);
				pak.WriteNonNullTerminatedString(rewardQuest.Name);
				pak.WritePascalString(rewardQuest.Description);
				int goalindex = 0;
				foreach (RewardQuest.QuestGoal goal in rewardQuest.Goals)
				{
					goalindex++;
					String goalDesc = String.Format("{0}\r", goal.Description);
					pak.WriteShortLowEndian((ushort)goalDesc.Length);
					pak.WriteNonNullTerminatedString(goalDesc);
					pak.WriteShortLowEndian((ushort)goal.ZoneID2);
					pak.WriteShortLowEndian((ushort)goal.XOffset2);
					pak.WriteShortLowEndian((ushort)goal.YOffset2);
					pak.WriteShortLowEndian(0x00);	// unknown
					pak.WriteShortLowEndian((ushort)goal.Type);
					pak.WriteShortLowEndian(0x00);	// unknown
					pak.WriteShortLowEndian((ushort)goal.ZoneID1);
					pak.WriteShortLowEndian((ushort)goal.XOffset1);
					pak.WriteShortLowEndian((ushort)goal.YOffset1);
					pak.WriteByte((byte)((goal.IsAchieved) ? 0x01 : 0x00));
					if (goal.QuestItem == null)
						pak.WriteByte(0x00);
					else
					{
						pak.WriteByte((byte)goalindex);
						WriteTemplateData(pak, goal.QuestItem, 1);
					}
				}
				SendTCP(pak);
				return;
				}
			}
			else
			{
				base.SendQuestPacket(quest, index);
			}
		}
	}
}
