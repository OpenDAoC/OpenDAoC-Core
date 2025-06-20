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
using DOL.Database;
using DOL.GS.Behaviour;
using DOL.GS.Quests;

namespace DOL.GS.PacketHandler
{
	[PacketLib(194, GameClient.eClientVersion.Version194)]
	public class PacketLib194 : PacketLib193
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);


		public override void SendQuestOfferWindow(GameNPC questNPC, GamePlayer player, DataQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, true);
		}

		public override void SendQuestRewardWindow(GameNPC questNPC, GamePlayer player, DataQuest quest)
		{
			SendQuestWindow(questNPC, player, quest, false);
		}

        const ushort MAX_STORY_LENGTH = 1000;   // Via trial and error, 1.108 client.
                                                // Often will cut off text around 990 but longer strings do not result in any errors. -Tolakram

		protected override void SendQuestWindow(GameNPC questNPC, GamePlayer player, DataQuest quest, bool offer)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.Dialog))))
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
				pak.WritePascalString(quest.Name);

				string personalizedSummary = BehaviourUtils.GetPersonalizedMessage(quest.Description, player);
				if (personalizedSummary.Length > 255)
				{
					pak.WritePascalString(personalizedSummary.Substring(0, 255)); // Summary is max 255 bytes or client will crash !
				}
				else
				{
					pak.WritePascalString(personalizedSummary);
				}

				if (offer)
				{
					string personalizedStory = BehaviourUtils.GetPersonalizedMessage(quest.Story, player);

					if (personalizedStory.Length > MAX_STORY_LENGTH)
					{
						pak.WriteShort(MAX_STORY_LENGTH);
						pak.WriteStringBytes(personalizedStory.Substring(0, MAX_STORY_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)personalizedStory.Length);
						pak.WriteStringBytes(personalizedStory);
					}
				}
				else
				{
					if (quest.FinishText.Length > MAX_STORY_LENGTH)
					{
						pak.WriteShort(MAX_STORY_LENGTH);
						pak.WriteStringBytes(quest.FinishText.Substring(0, MAX_STORY_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.FinishText.Length);
						pak.WriteStringBytes(quest.FinishText);
					}
				}

				pak.WriteShort(QuestID);
				pak.WriteByte((byte)quest.StepTexts.Count); // #goals count
				foreach (string text in quest.StepTexts)
				{
					string t = text;

					// Need to protect for any text length > 255.  It does not crash client but corrupts RewardQuest display -Tolakram
					if (text.Length > 253)
					{
						t = text.Substring(0, 253);
					}

					pak.WritePascalString(string.Format("{0}\r", t));
				}
				pak.WriteInt((uint)quest.MoneyReward());
				pak.WriteByte((byte)quest.ExperiencePercent(player));
				pak.WriteByte((byte)quest.FinalRewards.Count);
				foreach (DbItemTemplate reward in quest.FinalRewards)
				{
					WriteItemData(pak, GameInventoryItem.Create(reward));
				}
				pak.WriteByte(quest.NumOptionalRewardsChoice);
				pak.WriteByte((byte)quest.OptionalRewards.Count);
				foreach (DbItemTemplate reward in quest.OptionalRewards)
				{
					WriteItemData(pak, GameInventoryItem.Create(reward));
				}
				SendTCP(pak);
			}
		}

		protected override void SendQuestWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest,	bool offer)
		{
			using (GSTCPPacketOut pak = GSTCPPacketOut.GetForTick(p => p.Init(GetPacketCode(eServerPackets.Dialog))))
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

				string personalizedSummary = BehaviourUtils.GetPersonalizedMessage(quest.Summary, player);
				if (personalizedSummary.Length > 255)
					pak.WritePascalString(personalizedSummary.Substring(0, 255)); // Summary is max 255 bytes !
				else
					pak.WritePascalString(personalizedSummary);

				if (offer)
				{
					string personalizedStory = BehaviourUtils.GetPersonalizedMessage(quest.Story, player);

					if (personalizedStory.Length > ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteStringBytes(personalizedStory.Substring(0, ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)personalizedStory.Length);
						pak.WriteStringBytes(personalizedStory);
					}
				}
				else
				{
					if (quest.Conclusion.Length > (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteStringBytes(quest.Conclusion.Substring(0, (ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.Conclusion.Length);
						pak.WriteStringBytes(quest.Conclusion);
					}
				}

				pak.WriteShort(QuestID);
				pak.WriteByte((byte)quest.Goals.Count); // #goals count
				foreach (RewardQuest.QuestGoal goal in quest.Goals)
				{
					pak.WritePascalString(String.Format("{0}\r", goal.Description));
				}
				pak.WriteInt((uint)(quest.Rewards.Money)); // unknown, new in 1.94
				pak.WriteByte((byte)quest.Rewards.ExperiencePercent(player));
				pak.WriteByte((byte)quest.Rewards.BasicItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.BasicItems)
				{
					WriteItemData(pak, GameInventoryItem.Create(reward));
				}
				pak.WriteByte((byte)quest.Rewards.ChoiceOf);
				pak.WriteByte((byte)quest.Rewards.OptionalItems.Count);
				foreach (DbItemTemplate reward in quest.Rewards.OptionalItems)
				{
					WriteItemData(pak, GameInventoryItem.Create(reward));
				}
				SendTCP(pak);
			}
		}

		/// <summary>
		/// Constructs a new PacketLib for Version 1.94 clients
		/// </summary>
		/// <param name="client">the gameclient this lib is associated with</param>
		public PacketLib194(GameClient client)
			: base(client)
		{
		}
	}
}
