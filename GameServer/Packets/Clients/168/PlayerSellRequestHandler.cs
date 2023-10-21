using System;
using Core.Database.Tables;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.SellRequest, "Handles player selling", EClientStatus.PlayerInGame)]
public class PlayerSellRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		uint x = packet.ReadInt();
		uint y = packet.ReadInt();
		ushort id = packet.ReadShort();
		ushort item_slot = packet.ReadShort();

		if (client.Player.TargetObject == null)
		{
			client.Out.SendMessage("You must select an NPC to sell to.", EChatType.CT_Merchant, EChatLoc.CL_SystemWindow);
			return;
		}

		lock (client.Player.Inventory)
		{
			DbInventoryItem item = client.Player.Inventory.GetItem((EInventorySlot)item_slot);
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