using System.Collections;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.ModifyTrade, "Player Accepts Trade", EClientStatus.PlayerInGame)]
public class PlayerModifyTradeHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		byte isok = (byte)packet.ReadByte();
		byte repair = (byte)packet.ReadByte();
		byte combine = (byte)packet.ReadByte();
		packet.ReadByte(); // unknown

		ITradeWindow trade = client.Player.TradeWindow;
		if (trade == null)
			return;
		if (isok == 0)
		{
			trade.CloseTrade();
		}
		else if (isok == 1)
		{
			if (trade.Repairing != (repair == 1)) trade.Repairing = (repair == 1);
			if (trade.Combine != (combine == 1)) trade.Combine = (combine == 1);

			ArrayList tradeSlots = new ArrayList(10);
			for (int i = 0; i < 10; i++)
			{
				int slotPosition = packet.ReadByte();
				DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)slotPosition);
				if (item != null 
					&& ((item.IsDropable && item.IsTradable) || (client.Player.CanTradeAnyItem 
					|| trade is SelfCraftWindow
					|| (trade.Partner != null && trade.Partner.CanTradeAnyItem))))
				{
					tradeSlots.Add(item);
				}
			}
			trade.TradeItems = tradeSlots;

			packet.ReadShort();

			int[] tradeMoney = new int[5];
			for (int i = 0; i < 5; i++)
				tradeMoney[i] = packet.ReadShort();

			long money = MoneyMgr.GetMoney(tradeMoney[0], tradeMoney[1], tradeMoney[2], tradeMoney[3], tradeMoney[4]);
			trade.TradeMoney = money;

			trade.TradeUpdate();
		}
		else if (isok == 2)
		{
			trade.AcceptTrade();
		}
	}
}