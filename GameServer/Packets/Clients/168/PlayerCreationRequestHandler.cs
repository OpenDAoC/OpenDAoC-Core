using System.Reflection;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.CreatePlayerRequest, "Handles requests for players(0x7C) in game", EClientStatus.PlayerInGame)]
    public class PlayerCreationRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            ushort id = client.Version >= GameClient.eClientVersion.Version1126 ? packet.ReadShortLowEndian() : packet.ReadShort();
            GameClient target = ClientService.GetClientFromId(id);

            if (target == null)
            {
                if (Log.IsWarnEnabled)
                    Log.Warn($"Client {client.SessionID}:{client.TcpEndpointAddress} account {(client.Account == null ? "null" : client.Account.Name)} requested invalid client {id} --- disconnecting");

                client.Disconnect();
                return;
            }

            // DOLConsole.WriteLine("player creation request "+target.Player.Name);
            if (target.IsPlaying && target.Player != null && target.Player.ObjectState == GameObject.eObjectState.Active)
            {
                client.Out.SendPlayerCreate(target.Player);
                client.Out.SendLivingEquipmentUpdate(target.Player);
            }
        }
    }
}
