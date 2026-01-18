namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Called when player removes concentration spell in conc window
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.RemoveConcentrationEffect, "Handles Concentration Effect Remove Request", eClientStatus.PlayerInGame)]
    public class RemoveConcentrationEffectHandler : PacketHandler
    {
        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            int index = packet.ReadByte();
            GamePlayer player = client.Player;
            player.effectListComponent.StopConcentrationEffect(index, true);
        }
    }
}
