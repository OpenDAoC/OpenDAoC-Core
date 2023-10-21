using Core.Database;
using Core.GS.Housing;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PlayerAppraiseItemRequest, "Player Appraise Item Request handler.", EClientStatus.PlayerInGame)]
	public class PlayerAppraiseItemRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			uint X = packet.ReadInt();
			uint Y = packet.ReadInt();
			ushort id = packet.ReadShort();
			ushort item_slot = packet.ReadShort();

			new AppraiseActionHandler(client.Player, item_slot).Start(1);
		}

		/// <summary>
		/// Handles item apprise actions
		/// </summary>
		protected class AppraiseActionHandler : EcsGameTimerWrapperBase
		{
			/// <summary>
			/// The item slot
			/// </summary>
			protected readonly int m_slot;

			/// <summary>
			/// Constructs a new AppraiseAction
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="slot">The item slot</param>
			public AppraiseActionHandler(GamePlayer actionSource, int slot) : base(actionSource)
			{
				m_slot = slot;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(EcsGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;

				if (player.TargetObject == null)
					return 0;

				DbInventoryItem item = player.Inventory.GetItem((EInventorySlot) m_slot);

				if (player.TargetObject is GameMerchant merchant)
					merchant.OnPlayerAppraise(player, item, false);
				else if (player.TargetObject is GameLotMarker lot)
					lot.OnPlayerAppraise(player, item, false);

				return 0;
			}
		}
	}
}
