namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.MinotaurRelicWindow, "Handles Relic window commands", eClientStatus.PlayerInGame)]
    public class MinotaurRelicWindowHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            // todo
        }
    }
}