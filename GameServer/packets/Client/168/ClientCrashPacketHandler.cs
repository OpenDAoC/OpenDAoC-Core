using System.Reflection;
using DOL.Logging;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ClientCrash, "Handles client crash packets", eClientStatus.None)]
    public class ClientCrashPacketHandler : PacketHandler
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            string dllName = packet.ReadString(16);
            packet.Position = 0x50;
            uint upTime = packet.ReadInt();
            string text = $"Client crash ({client}) dll:{dllName} clientUptime:{upTime}sec";

            if (log.IsInfoEnabled)
                log.Info(text);

            client.Out.SendPlayerQuit(true);
            client.Disconnect();
        }
    }
}
