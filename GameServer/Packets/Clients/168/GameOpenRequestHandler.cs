using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.GameOpenRequest, "Checks if UDP is working for the client", EClientStatus.None)]
public class GameOpenRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		int flag = packet.ReadByte(); // Always 0? (1.127)
		client.UdpPingTime = GameLoopMgr.GetCurrentTime();
		client.UdpConfirm = flag == 1;
		client.Out.SendGameOpenReply();
		client.Out.SendStatusUpdate(); // based on 1.74 logs
		client.Out.SendUpdatePoints(); // based on 1.74 logs
		client.Player?.UpdateDisabledSkills(); // based on 1.74 logs
	}
}