namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.PlayerSitRequest, "Handles Player Sit Request.", EClientStatus.PlayerInGame)]
	public class PlayerSitRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			var status = (byte) packet.ReadByte();

			new SitRequestHandler(client.Player, status != 0x00).Start(1);
		}

		/// <summary>
		/// Handles player sit requests
		/// </summary>
		protected class SitRequestHandler : ECSGameTimerWrapperBase
		{
			/// <summary>
			/// The new sit state
			/// </summary>
			protected readonly bool m_sit;

			/// <summary>
			/// Constructs a new SitRequestHandler
			/// </summary>
			/// <param name="actionSource">The action source</param>
			/// <param name="sit">The new sit state</param>
			public SitRequestHandler(GamePlayer actionSource, bool sit) : base(actionSource)
			{
				m_sit = sit;
			}

			/// <summary>
			/// Called on every timer tick
			/// </summary>
			protected override int OnTick(ECSGameTimer timer)
			{
				GamePlayer player = (GamePlayer) timer.Owner;
				player.Sit(m_sit);
				return 0;
			}
		}
	}
}
