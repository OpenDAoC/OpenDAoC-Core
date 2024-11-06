using DOL.Events;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handler for quest reward dialog response.
    /// </summary>
    /// <author>Aredhel</author>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.QuestRewardChosen, "Quest Reward Choosing Handler", eClientStatus.PlayerInGame)]
    public class QuestRewardChosenHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            byte response = (byte) packet.ReadByte();

            if (response != 1) // Confirm.
                return;

            byte countChosen = (byte) packet.ReadByte();
            int[] itemsChosen = new int[8];

            for (int i = 0; i < 8; ++i)
                itemsChosen[i] = packet.ReadByte();

            ushort data2 = packet.ReadShort(); // Unknown.
            ushort data3 = packet.ReadShort(); // Unknown.
            ushort data4 = packet.ReadShort(); // Unknown.
            ushort questId = packet.ReadShort();
            ushort questGiverId = packet.ReadShort();
            GamePlayer player = client.Player;
            player.Notify(GamePlayerEvent.QuestRewardChosen, player, new QuestRewardChosenEventArgs(questGiverId, questId, countChosen, itemsChosen));
        }
    }
}
