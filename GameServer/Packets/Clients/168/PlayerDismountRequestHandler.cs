namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PlayerDismountRequest, "Handles Player Dismount Request.", EClientStatus.PlayerInGame)]
	public class PlayerDismountRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			new DismountRequestHandler(client.Player).Start(1);
		}

		/// <summary>
		/// Handles player dismount requests
		/// </summary>
		protected class DismountRequestHandler : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// Constructs a new DismountRequestHandler
			/// </summary>
			/// <param name="actionSource"></param>
			public DismountRequestHandler(GamePlayer actionSource) : base(actionSource)
			{
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;

				if (!player.IsRiding)
				{
					ChatUtil.SendSystemMessage(player, "You are not riding any steed!");
					return 0;
				}

				player.DismountSteed(false);
				return 0;
			}
		}
	}
}
