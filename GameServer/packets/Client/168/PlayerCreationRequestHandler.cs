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

            if (!target.IsPlaying)
                return;

            GamePlayer playerTarget = target.Player;

            if (playerTarget.ObjectState is not GameObject.eObjectState.Active)
                return;

            GamePlayer player = client.Player;

            if (!player.CanDetect(playerTarget) || !player.IsWithinRadius(playerTarget, WorldMgr.VISIBILITY_DISTANCE))
                return;

            client.Out.SendPlayerCreate(playerTarget);
            client.Out.SendLivingEquipmentUpdate(playerTarget);
        }
    }
}
