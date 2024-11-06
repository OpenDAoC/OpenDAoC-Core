namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Called when player removes concentration spell in conc window
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.RemoveConcentrationEffect, "Handles Concentration Effect Remove Request", eClientStatus.PlayerInGame)]
    public class RemoveConcentrationEffectHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int index = packet.ReadByte();
            GamePlayer player = client.Player;

            lock (player.effectListComponent.ConcentrationEffectsLock)
            {
                if (index < player.effectListComponent.ConcentrationEffects.Count)
                    EffectService.RequestImmediateCancelConcEffect(player.effectListComponent.ConcentrationEffects[index], true);
            }
        }
    }
}
