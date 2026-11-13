using System;
using System.Collections.Generic;
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

		/// <summary>
		/// Player temp property holding the rewards of the last quest window sent: the basic rewards, then the optional ones.
		/// The client sends a reward's position back when the player delves its icon (DetailDisplayHandler, type 29).
		/// </summary>
		public const string QUEST_WINDOW_REWARDS_PROPERTY = "QuestWindowRewards";


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
			using (var pak = GameLoop.GetObjectForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.Dialog)))
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
					pak.WritePascalString(personalizedSummary.AsSpan(0, 255)); // Summary is max 255 bytes or client will crash !
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
						pak.WriteNonNullTerminatedString(personalizedStory.AsSpan(0, MAX_STORY_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)personalizedStory.Length);
						pak.WriteNonNullTerminatedString(personalizedStory);
					}
				}
				else
				{
					if (quest.FinishText.Length > MAX_STORY_LENGTH)
					{
						pak.WriteShort(MAX_STORY_LENGTH);
						pak.WriteNonNullTerminatedString(quest.FinishText.AsSpan(0, MAX_STORY_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)quest.FinishText.Length);
						pak.WriteNonNullTerminatedString(quest.FinishText);
					}
				}

				pak.WriteShort(QuestID);
				pak.WriteByte((byte)quest.StepTexts.Count); // #goals count
				Span<char> buffer = stackalloc char[254]; // 253 + 1 for '\r'

				foreach (string text in quest.StepTexts)
				{
					ReadOnlySpan<char> textSpan = text == null ? [] : text;

					if (textSpan.Length > 253)
						textSpan = textSpan[..253];

					textSpan.CopyTo(buffer);
					buffer[textSpan.Length] = '\r';
					pak.WritePascalString(buffer[..(textSpan.Length + 1)]);
				}

				pak.WriteInt((uint)quest.MoneyReward());
				pak.WriteByte((byte)quest.ExperiencePercent(player));
				WriteQuestRewards(pak, player, quest.FinalRewards, quest.NumOptionalRewardsChoice, quest.OptionalRewards);
				SendTCP(pak);
			}
		}

		protected override void SendQuestWindow(GameNPC questNPC, GamePlayer player, RewardQuest quest,	bool offer)
		{
			using (var pak = GameLoop.GetObjectForTick<GSTCPPacketOut>().Init(GetPacketCode(eServerPackets.Dialog)))
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
					pak.WritePascalString(personalizedSummary.AsSpan(0, 255)); // Summary is max 255 bytes !
				else
					pak.WritePascalString(personalizedSummary);

				if (offer)
				{
					string personalizedStory = BehaviourUtils.GetPersonalizedMessage(quest.Story, player);

					if (personalizedStory.Length > ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH)
					{
						pak.WriteShort((ushort)ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH);
						pak.WriteNonNullTerminatedString(personalizedStory.AsSpan(0, ServerProperties.Properties.MAX_REWARDQUEST_DESCRIPTION_LENGTH));
					}
					else
					{
						pak.WriteShort((ushort)personalizedStory.Length);
						pak.WriteNonNullTerminatedString(personalizedStory);
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
				pak.WriteInt((uint)(quest.Rewards.Money)); // unknown, new in 1.94
				pak.WriteByte((byte)quest.Rewards.ExperiencePercent(player));
				WriteQuestRewards(pak, player, quest.Rewards.BasicItems, (byte)quest.Rewards.ChoiceOf, quest.Rewards.OptionalItems);
				SendTCP(pak);
			}
		}

		/// <summary>
		/// Writes the basic and optional reward lists of a quest window.
		/// The first u16 of each item block is echoed back by the client when the player delves that reward
		/// (detail request type 29), so it is set to 1 + the reward's position: basic rewards first, then optional ones.
		/// The rewards are kept on the player so the delve can find them.
		/// </summary>
		protected virtual void WriteQuestRewards(GSTCPPacketOut pak, GamePlayer player, IList<DbItemTemplate> basicRewards, byte optionalChoiceCount, IList<DbItemTemplate> optionalRewards)
		{
			List<DbItemTemplate> rewards = new(basicRewards.Count + optionalRewards.Count);

			pak.WriteByte((byte) basicRewards.Count);

			foreach (DbItemTemplate reward in basicRewards)
				WriteQuestRewardItem(pak, reward, rewards);

			pak.WriteByte(optionalChoiceCount);
			pak.WriteByte((byte) optionalRewards.Count);

			foreach (DbItemTemplate reward in optionalRewards)
				WriteQuestRewardItem(pak, reward, rewards);

			player.TempProperties.SetProperty(QUEST_WINDOW_REWARDS_PROPERTY, rewards);
		}

		private void WriteQuestRewardItem(GSTCPPacketOut pak, DbItemTemplate reward, List<DbItemTemplate> rewards)
		{
			rewards.Add(reward);
			long start = pak.Position;
			WriteItemData(pak, GameInventoryItem.Create(reward));
			long end = pak.Position;
			pak.Position = start;
			pak.WriteShort((ushort) rewards.Count);
			pak.Position = end;
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
