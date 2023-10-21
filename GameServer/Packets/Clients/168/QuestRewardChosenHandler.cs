using Core.Events;

namespace Core.GS.PacketHandler.Client.v168
{
	/// <summary>
	/// Handler for quest reward dialog response.
	/// </summary>
	/// <author>Aredhel</author>
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.QuestRewardChosen, "Quest Reward Choosing Handler", EClientStatus.PlayerInGame)]
	public class QuestRewardChosenHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			var response = (byte) packet.ReadByte();
			if (response != 1) // confirm
				return;

			var countChosen = (byte) packet.ReadByte();

			var itemsChosen = new int[8];
			for (int i = 0; i < 8; ++i)
				itemsChosen[i] = packet.ReadByte();

			ushort data2 = packet.ReadShort(); // unknown
			ushort data3 = packet.ReadShort(); // unknown
			ushort data4 = packet.ReadShort(); // unknown

			ushort questID = packet.ReadShort();
			ushort questGiverID = packet.ReadShort();

			new QuestRewardChosenAction(client.Player, countChosen, itemsChosen, questGiverID, questID).Start(1);
		}

		/// <summary>
		/// Send dialog response via Notify().
		/// </summary>
		protected class QuestRewardChosenAction : EcsGameTimerWrapperBase
		{
			private readonly int m_countChosen;
			private readonly int[] m_itemsChosen;
			private readonly int m_questGiverID;
			private readonly int m_questID;

			/// <summary>
			/// Constructs a new QuestRewardChosenAction.
			/// </summary>
			/// <param name="actionSource">The responding player,</param>
			/// <param name="countChosen">Number of items chosen from the dialog.</param>
			/// <param name="itemsChosen">List of items chosen from the dialog.</param>
			/// <param name="questGiverID">ID of the quest NPC.</param>
			/// <param name="questID">ID of the quest.</param>
			public QuestRewardChosenAction(GamePlayer actionSource, int countChosen, int[] itemsChosen, int questGiverID, int questID) : base(actionSource)
			{
				m_countChosen = countChosen;
				m_itemsChosen = itemsChosen;
				m_questGiverID = questGiverID;
				m_questID = questID;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;
				player.Notify(GamePlayerEvent.QuestRewardChosen, player, new QuestRewardChosenEventArgs(m_questGiverID, m_questID, m_countChosen, m_itemsChosen));
				return 0;
			}
		}
	}
}
