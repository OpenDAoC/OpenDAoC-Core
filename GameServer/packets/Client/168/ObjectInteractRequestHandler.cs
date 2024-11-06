namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ObjectInteractRequest, "Handles Client Interact Request", eClientStatus.PlayerInGame)]
    public class ObjectInteractRequestHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            // TODO: Utilize these client-sent coordinates to possibly check for exploits which are spoofing position packets but not spoofing them everywhere.
            uint playerX = packet.ReadInt();
            uint playerY = packet.ReadInt();
            int sessionId = packet.ReadShort();
            ushort targetOid = packet.ReadShort();
            GamePlayer player = client.Player;
            Region region = player.CurrentRegion;

            if (region == null)
                return;

            GameObject targeObject = region.GetObject(targetOid);

            if (targeObject == null || !player.IsWithinRadius(targeObject, WorldMgr.OBJ_UPDATE_DISTANCE))
                player.Out.SendObjectDelete(targetOid);
            else
                targeObject.Interact(player);

            return;
        }
    }
}
