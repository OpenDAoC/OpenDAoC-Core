using System;
using DOL.Database;
using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.SellRequest, "Handles player selling", eClientStatus.PlayerInGame)]
	public class PlayerSellRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			uint x = packet.ReadInt();
			uint y = packet.ReadInt();
			ushort id = packet.ReadShort();
			ushort item_slot = packet.ReadShort();

			if (client.Player.TargetObject == null)
			{
				client.Out.SendMessage("You must select an NPC to sell to.", eChatType.CT_Merchant, eChatLoc.CL_SystemWindow);
				return;
			}

			lock (client.Player.Inventory.Lock)
			{
				DbInventoryItem item = client.Player.Inventory.GetItem((eInventorySlot)item_slot);
				if (item == null)
					return;

				int itemCount = Math.Max(1, item.Count);
				int packSize = Math.Max(1, item.PackSize);

				if (client.Player.TargetObject is GameMerchant merchant)
				{
					//Let the merchant choos how to handle the trade.
					merchant.OnPlayerSell(client.Player, item);

				}
				else if (client.Player.TargetObject is GameGuardMerchant guardMerchant)
				{
					guardMerchant.OnPlayerSell(client.Player, item);
				}
				else if (client.Player.TargetObject is GameItemCurrencyGuardMerchant guardCurrencyMerchant)
				{
					guardCurrencyMerchant.OnPlayerSell(client.Player, item);
				}
				else if (client.Player.TargetObject is GameLotMarker lot)
				{
					lot.OnPlayerSell(client.Player, item);
				}
			}
		}
	}
}