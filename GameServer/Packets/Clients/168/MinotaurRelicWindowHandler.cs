using System.Reflection;
using Core.GS.Enums;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.MinotaurRelicWindow, "Handles Relic window commands", EClientStatus.PlayerInGame)]
    public class MinotaurRelicWindowHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            // todo
        }
    }
}