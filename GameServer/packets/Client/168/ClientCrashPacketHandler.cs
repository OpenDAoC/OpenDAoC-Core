using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Network;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ClientCrash, "Handles client crash packets", eClientStatus.None)]
    public class ClientCrashPacketHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            string dllName = packet.ReadString(16);
            packet.Position = 0x50;
            uint upTime = packet.ReadInt();
            string text = $"Client crash ({client}) dll:{dllName} clientUptime:{upTime}sec";

            if (log.IsInfoEnabled)
                log.Info(text);

            if (log.IsDebugEnabled)
            {
                if (Properties.SAVE_PACKETS)
                {
                    log.Debug("Last client sent/received packets (from older to newer):");

                    foreach (IPacket prevPak in client.PacketProcessor.GetLastPackets())
                        log.Debug(prevPak.ToHumanReadable());
                }
                else
                    log.Debug($"Enable the server property {nameof(Properties.SAVE_PACKETS)} to see the last few sent/received packets.");
            }

            client.Out.SendPlayerQuit(true);
            client.Disconnect();
        }
    }
}
