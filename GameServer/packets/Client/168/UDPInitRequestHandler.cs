namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.UDP, eClientPackets.UDPInitRequest, "Handles UDP init", eClientStatus.None)]
	public class UDPInitRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
