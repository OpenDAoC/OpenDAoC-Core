using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.RemoveQuestRequest, "Quest Remove request Handler.", EClientStatus.PlayerInGame)]
public class QuestRemoveRequestHandler : IPacketHandler
{
    public void HandlePacket(GameClient client, GsPacketIn packet)
    {
        _ = packet.ReadShort();
        ushort questIndex = packet.ReadShort();
        _ = packet.ReadShort();
        _ = packet.ReadShort();

        foreach (var entry in client.Player.QuestList)
        {
            if (questIndex == entry.Value)
                entry.Key.AbortQuest();
        }
    }
}