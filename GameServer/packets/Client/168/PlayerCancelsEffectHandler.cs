using DOL.GS.Effects;

namespace DOL.GS.PacketHandler.Client.v168
{
    /// <summary>
    /// Handles effect cancel requests
    /// </summary>
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.PlayerCancelsEffect, "Handle Player Effect Cancel Request.", eClientStatus.PlayerInGame)]
    public class PlayerCancelsEffectHandler : IPacketHandler
    {
        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            int effectId = packet.ReadShort();
            GamePlayer player = client.Player;

            if (client.Version >= GameClient.eClientVersion.Version1110)
                CancelEffect1110();
            else
                CancelEffect();

            void CancelEffect1110()
            {
                EffectListComponent effectListComponent = player.effectListComponent;
                ECSGameEffect effect = effectListComponent.TryGetEffectFromEffectId(effectId);

                if (effect != null)
                    EffectService.RequestImmediateCancelEffect(effect, true);

                return;
            }

            void CancelEffect()
            {
                // Outdated.
                IGameEffect found = null;

                lock (player.EffectList)
                {
                    foreach (IGameEffect effect in player.EffectList)
                    {
                        if (effect.InternalID == effectId)
                        {
                            found = effect;
                            break;
                        }
                    }
                }

                found?.Cancel(true);
            }
        }
    }
}
