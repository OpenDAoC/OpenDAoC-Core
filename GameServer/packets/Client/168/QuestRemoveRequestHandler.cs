namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.RemoveQuestRequest, "Quest Remove request Handler.", eClientStatus.PlayerInGame)]
    public class QuestRemoveRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            _ = packet.ReadShort();
            ushort questIndex = packet.ReadShort();
            _ = packet.ReadShort();
            _ = packet.ReadShort();

            foreach (var entry in client.Player.QuestList)
            {
                if (questIndex != entry.Value)
                    continue;

                entry.Key.AbortQuest();
                return;
            }
        }
    }
}
