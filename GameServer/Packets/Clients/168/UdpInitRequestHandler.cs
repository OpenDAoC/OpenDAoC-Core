namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.UDP, EClientPackets.UDPInitRequest, "Handles UDP init", EClientStatus.None)]
	public class UdpInitRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			string localIP;
			ushort localPort;
			if (client.Version >= GameClient.eClientVersion.Version1124)
			{
				localIP = packet.ReadString(20);
				localPort = packet.ReadShort();
			}
			else
			{
				localIP = packet.ReadString(22);
				localPort = packet.ReadShort();
			}
			client.LocalIP = localIP;
			// client.UdpEndPoint = new IPEndPoint(IPAddress.Parse(localIP), localPort);
			client.Out.SendUDPInitReply();
		}
	}
}
