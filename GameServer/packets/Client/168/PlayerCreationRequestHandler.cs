using System.Reflection;
using DOL.Logging;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandler(PacketHandlerType.TCP, eClientPackets.CreatePlayerRequest, "Handles requests for players(0x7C) in game", eClientStatus.PlayerInGame)]
    public class PlayerCreationRequestHandler : PacketHandler
    {
        private static readonly Logger Log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            ushort id = client.Version >= GameClient.eClientVersion.Version1126 ? packet.ReadShortLowEndian() : packet.ReadShort();
            GameClient target = ClientService.Instance.GetClientBySessionId(id);

            if (target == null)
            {
                if (Log.IsWarnEnabled)
                    Log.Warn($"Client {client.SessionID}:{client.TcpEndpointAddress} account {(client.Account == null ? "null" : client.Account.Name)} requested invalid client {id}");

                // Uncomment if this is spammed, but try not to disconnect if id == 0.
                // client.Disconnect();
                return;
            }

            if (target.IsPlaying && target.Player != null && target.Player.ObjectState == GameObject.eObjectState.Active)
            {
                client.Out.SendPlayerCreate(target.Player);
                client.Out.SendLivingEquipmentUpdate(target.Player);
            }
        }
    }
}
