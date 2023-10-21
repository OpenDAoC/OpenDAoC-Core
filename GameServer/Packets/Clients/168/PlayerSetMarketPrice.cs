using System.Reflection;
using log4net;

namespace Core.GS.PacketHandler.Client.v168
{
    [PacketHandler(EPacketHandlerType.TCP, EClientPackets.SetMarketPrice, "Set Market/Consignment Merchant Price.", EClientStatus.PlayerInGame)]
    public class PlayerSetMarketPriceHandler : IPacketHandler
    {
        /// <summary>
        /// Defines a logger for this class.
        /// </summary>
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public void HandlePacket(GameClient client, GsPacketIn packet)
        {
            if (client == null || client.Player == null)
                return;

			int slot = packet.ReadByte();
			int unk1 = packet.ReadByte();
			ushort unk2 = packet.ReadShort();
			uint price = packet.ReadInt();

			// only IGameInventoryObjects can handle set price commands
			if (client.Player.TargetObject == null || (client.Player.TargetObject is IGameInventoryObject) == false)
				return;

			(client.Player.TargetObject as IGameInventoryObject).SetSellPrice(client.Player, (ushort)slot, price);
        }
    }
}