using System.Reflection;
using Core.Base;
using Core.GS.Enums;
using Core.GS.Packets.Server;
using Core.GS.ServerProperties;
using log4net;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.ClientCrash, "Handles client crash packets", EClientStatus.None)]
public class ClientCrashPacketHandler : IPacketHandler
{
    /// <summary>
    /// Defines a logger for this class.
    /// </summary>
    private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

    public void HandlePacket(GameClient client, GsPacketIn packet)
    {
        string dllName = packet.ReadString(16);
        packet.Position = 0x50;
        uint upTime = packet.ReadInt();
        string text = $"Client crash ({client}) dll:{dllName} clientUptime:{upTime}sec";
        log.Info(text);

        if (log.IsDebugEnabled)
        {
            if (Properties.SAVE_PACKETS)
            {
                log.Debug("Last client sent/received packets (from older to newer):");

                foreach (IPacket prevPak in client.PacketProcessor.GetLastPackets())
                    log.Info(prevPak.ToHumanReadable());
            }
            else
                log.Info($"Enable the server property {nameof(Properties.SAVE_PACKETS)} to see the last few sent/received packets.");
        }

        client.Out.SendPlayerQuit(true);

        if (client.Player != null)
        {
            client.Player.SaveIntoDatabase();
            client.Player.Quit(true);
        }

        client.Disconnect();
    }
}