namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandler(PacketHandlerType.TCP, eClientPackets.CharacterSelectRequest, "Handles setting SessionID", eClientStatus.LoggedIn)]
    public class CharacterSelectRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            // Let's just ignore this packet's content and instantiate the player in `WorldInitRequestHandler` instead.
            // Older clients can send the character name, more recent versions don't seem to do that anymore.

            // packet.Skip(4);
            // packet.Skip(1);
            // string charName = packet.ReadString(28);

            client.Out.SendLoginGranted();
            client.Out.SendSessionID();
        }
    }
}
