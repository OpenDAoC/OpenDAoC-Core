using Core.GS.ECS;
using Core.GS.Enums;

namespace Core.GS.Packets.Clients;

/// <summary>
/// Handles spell cast requests from client
/// </summary>
[PacketHandler(EPacketHandlerType.TCP, EClientPackets.UseSlot, "Handle Player Use Slot Request.", EClientStatus.PlayerInGame)]
public class UseSlotHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		if (client.Version >= GameClient.eClientVersion.Version1124)
		{
			client.Player.X = (int)packet.ReadFloatLowEndian();
			client.Player.Y = (int)packet.ReadFloatLowEndian();
			client.Player.Z = (int)packet.ReadFloatLowEndian();
			client.Player.CurrentSpeed = (short)packet.ReadFloatLowEndian();
			client.Player.Heading = packet.ReadShort();
		}
		int flagSpeedData = packet.ReadShort();
		int slot = packet.ReadByte();
		int type = packet.ReadByte();

		new UseSlotAction(client.Player, flagSpeedData, slot, type).Start(1);
	}

	/// <summary>
	/// Handles player use slot actions
	/// </summary>
	protected class UseSlotAction : EcsGameTimerWrapperBase
	{
		/// <summary>
		/// The speed and flags data
		/// </summary>
		protected readonly int m_flagSpeedData;

		/// <summary>
		/// The slot index
		/// </summary>
		protected readonly int m_slot;

		/// <summary>
		/// The use type
		/// </summary>
		protected readonly int m_useType;

		/// <summary>
		/// Constructs a new UseSlotAction
		/// </summary>
		/// <param name="actionSource">The action source</param>
		/// <param name="flagSpeedData">The speed and flags data</param>
		/// <param name="slot">The slot index</param>
		/// <param name="useType">The use type</param>
		public UseSlotAction(GamePlayer actionSource, int flagSpeedData, int slot, int useType) : base(actionSource)
		{
			m_flagSpeedData = flagSpeedData;
			m_slot = slot;
			m_useType = useType;
		}

		/// <summary>
		/// Called on every timer tick
		/// </summary>
		protected override int OnTick(EcsGameTimer timer)
		{
			GamePlayer player = (GamePlayer) timer.Owner;

			// Commenting out. 'flagSpeedData' doesn't vary with movement speed, and this stops the player for a fraction of a second.
			//if ((m_flagSpeedData & 0x200) != 0)
			//{
			//	player.CurrentSpeed = (short)(-(m_flagSpeedData & 0x1ff)); // backward movement
			//}
			//else
			//{
			//	player.CurrentSpeed = (short)(m_flagSpeedData & 0x1ff); // forwardmovement
			//}

			player.IsStrafing = (m_flagSpeedData & 0x4000) != 0;
			player.TargetInView = (m_flagSpeedData & 0xa000) != 0; // why 2 bits? that has to be figured out
			player.GroundTargetInView = ((m_flagSpeedData & 0x1000) != 0);
			player.UseSlot(m_slot, m_useType);
			return 0;
		}
	}
}