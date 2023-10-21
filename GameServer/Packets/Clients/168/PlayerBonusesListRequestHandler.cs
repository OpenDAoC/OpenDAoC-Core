using System.Linq;
using System.Reflection;
using Core.Language;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
	[PacketHandler(EPacketHandlerType.TCP, EClientPackets.BonusesListRequest, "Handles player bonuses button clicks", EClientStatus.PlayerInGame)]
	public class PlayerBonusesListRequestHandler : IPacketHandler
	{
		/// <summary>
		/// Defines a logger for this class.
		/// </summary>
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public void HandlePacket(GameClient client, GsPacketIn packet)
		{
			if (client.Player == null)
				return;

			int code = packet.ReadByte();
			if (code != 0)
				log.Warn($"bonuses button: code is other than zero ({code})");

			new EcsGameTimer(client.Player, new EcsGameTimer.EcsTimerCallback(_ =>
			{
				client.Player.Out.SendCustomTextWindow(LanguageMgr.GetTranslation(client.Account.Language, "PlayerBonusesListRequestHandler.HandlePacket.Bonuses"), client.Player.GetBonuses().ToList());
				return 0;
			}), 1);
		}
	}
}
