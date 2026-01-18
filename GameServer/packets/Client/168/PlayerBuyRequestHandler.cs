using DOL.GS.Housing;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.BuyRequest, "Handles player buy", eClientStatus.PlayerInGame)]
	public class PlayerBuyRequestHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			if (client.Player == null)
				return;

			uint X = packet.ReadInt();
			uint Y = packet.ReadInt();
			ushort id = packet.ReadShort();
			ushort item_slot = packet.ReadShort();
			byte item_count = (byte)packet.ReadByte();
			byte menu_id = (byte)packet.ReadByte();

			switch ((eMerchantWindowType)menu_id)
			{
				case eMerchantWindowType.HousingInsideShop:
				case eMerchantWindowType.HousingOutsideShop:
				case eMerchantWindowType.HousingBindstoneHookpoint:
				case eMerchantWindowType.HousingCraftingHookpoint:
				case eMerchantWindowType.HousingNPCHookpoint:
				case eMerchantWindowType.HousingVaultHookpoint:
				case eMerchantWindowType.HousingDeedMenu:
					{
						HouseMgr.BuyHousingItem(client.Player, item_slot, item_count, (eMerchantWindowType)menu_id);
						break;
					}
				default:
					{
						if (client.Player.TargetObject == null)
							return;

						//Forward the buy process to the merchant
						if (client.Player.TargetObject is GameMerchant merchant)
						{
							//Let merchant choose what happens
							merchant.OnPlayerBuy(client.Player, item_slot, item_count);
						}
						else if (client.Player.TargetObject is GameGuardMerchant guardMerchant)
						{
							guardMerchant.OnPlayerBuy(client.Player, item_slot, item_count);
						}
						else if (client.Player.TargetObject is GameItemCurrencyGuardMerchant guardCurrencyMerchant)
						{
							guardCurrencyMerchant.OnPlayerBuy(client.Player, item_slot, item_count);
						}
						else if (client.Player.TargetObject is GameLotMarker lot)
						{
							lot.OnPlayerBuy(client.Player, item_slot, item_count);
						}
						break;
					}
			}
		}
	}
}