namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.RemoveQuestRequest, "Quest Remove request Handler.", eClientStatus.PlayerInGame)]
    public class QuestRemoveRequestHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            _ = packet.ReadShort();
            ushort questIndex = packet.ReadShort();
            _ = packet.ReadShort();
            _ = packet.ReadShort();

            client.Player.GetQuestByIndex(questIndex)?.AbortQuest();
        }
    }
}
