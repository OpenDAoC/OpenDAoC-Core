namespace DOL.GS.PacketHandler
{
    [PacketLib(1130, GameClient.eClientVersion.Version1130)]
    public class PacketLib1130 : PacketLib1129
    {
        public PacketLib1130(GameClient client) : base(client) { }
    }
}
