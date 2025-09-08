namespace DOL.GS.PacketHandler
{
    [PacketLib(1129, GameClient.eClientVersion.Version1129)]
    public class PacketLib1129 : PacketLib1128
    {
        public PacketLib1129(GameClient client) : base(client) { }
    }
}
