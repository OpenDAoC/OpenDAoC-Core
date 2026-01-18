using System.Collections;
using DOL.Database;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.ModifyTrade, "Player Accepts Trade", eClientStatus.PlayerInGame)]
	public class PlayerModifyTradeHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
					DbInventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)slotPosition);
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

				long money = Money.GetMoney(tradeMoney[0], tradeMoney[1], tradeMoney[2], tradeMoney[3], tradeMoney[4]);
				trade.TradeMoney = money;

				trade.TradeUpdate();
			}
			else if (isok == 2)
			{
				trade.AcceptTrade();
			}
		}
	}
}

