namespace DOL.GS.PacketHandler.Client.v168
{
	/// <summary>
	/// Handles the ping packet
	/// </summary>
	[PacketHandlerAttribute(PacketHandlerType.UDP, eClientPackets.UDPPingRequest, "Sends the UDP Init reply", eClientStatus.None)]
	public class UDPPingRequestHandler : PacketHandler
	{
		/// <summary>
		/// Called when the packet has been received
		/// </summary>
		/// <param name="client">Client that sent the packet</param>
		/// <param name="packet">Packet data</param>
		/// <returns>Non zero if function was successful</returns>
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			if (client.Version < GameClient.eClientVersion.Version1124)
			{
				string localIP = packet.ReadString(22);
				ushort localPort = packet.ReadShort();
				// TODO check changed localIP
				client.LocalIP = localIP;
			}
			// unsure what this value is now thats sent in 1.125
			// Its just a ping back letting the server know that UDP connection is still alive
			client.UdpPingTime = GameLoop.GameLoopTime;
			client.UdpConfirm = true;
		}
	}
}
