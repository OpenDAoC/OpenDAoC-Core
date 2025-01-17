using System.Linq;
using System.Reflection;
using DOL.Language;

namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.BonusesListRequest, "Handles player bonuses button clicks", eClientStatus.PlayerInGame)]
    public class PlayerBonusesListRequestHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly Logging.Logger log = Logging.LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GSPacketIn packet)
        {
            if (client.Player == null)
                return;

            int code = packet.ReadByte();

            if (code != 0)
                log.Warn($"Bonuses button: code is other than zero ({code})");

            client.Player.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Bonuses"), client.Player.GetBonuses().ToList());
        }
    }
}
