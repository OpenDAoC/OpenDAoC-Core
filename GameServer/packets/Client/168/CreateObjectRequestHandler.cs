namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CreateObjectRequest, "Handles creation packet requests for objects", eClientStatus.PlayerInGame)]
    public class CreateObjectRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Player == null)
                return;

            Region region = client.Player.CurrentRegion;

            if (region == null)
                return;

            ushort id;

            if (client.Version >= GameClient.eClientVersion.Version1126)
                id = packet.ReadShortLowEndian(); // Dre: disassembled game.dll show a write of uint, is it a wip in the game.dll?
            else
                id = packet.ReadShort();

            GameObject obj = region.GetObject(id);

            if (obj == null || !client.Player.IsWithinRadius(obj, WorldMgr.OBJ_UPDATE_DISTANCE))
            {
                client.Out.SendObjectDelete(id);
                return;
            }

            ClientService.CreateObjectForPlayer(client.Player, obj);
        }
    }
}
