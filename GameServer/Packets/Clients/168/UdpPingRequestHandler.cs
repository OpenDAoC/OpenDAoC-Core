using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

/// <summary>
/// Handles the ping packet
/// </summary>
[PacketHandler(EPacketHandlerType.UDP, EClientPackets.UDPPingRequest, "Sends the UDP Init reply", EClientStatus.None)]
public class UdpPingRequestHandler : IPacketHandler
{
	/// <summary>
	/// Called when the packet has been received
	/// </summary>
	/// <param name="client">Client that sent the packet</param>
	/// <param name="packet">Packet data</param>
	/// <returns>Non zero if function was successful</returns>
	public void HandlePacket(GameClient client, GsPacketIn packet)
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
		client.UdpPingTime = GameLoopMgr.GetCurrentTime();
		client.UdpConfirm = true;
	}
}