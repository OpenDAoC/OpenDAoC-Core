using System.Reflection;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using log4net;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.BuyRequest, "Handles player buy", EClientStatus.PlayerInGame)]
public class PlayerBuyRequestHandler : IPacketHandler
{
	/// <summary>
	/// Defines a logger for this class.
	/// </summary>
	private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		if (client.Player == null)
			return;

		uint X = packet.ReadInt();
		uint Y = packet.ReadInt();
		ushort id = packet.ReadShort();
		ushort item_slot = packet.ReadShort();
		byte item_count = (byte)packet.ReadByte();
		byte menu_id = (byte)packet.ReadByte();

		switch ((EMerchantWindowType)menu_id)
		{
			case EMerchantWindowType.HousingInsideShop:
			case EMerchantWindowType.HousingOutsideShop:
			case EMerchantWindowType.HousingBindstoneHookpoint:
			case EMerchantWindowType.HousingCraftingHookpoint:
			case EMerchantWindowType.HousingNPCHookpoint:
			case EMerchantWindowType.HousingVaultHookpoint:
			case EMerchantWindowType.HousingDeedMenu:
				{
					HouseMgr.BuyHousingItem(client.Player, item_slot, item_count, (EMerchantWindowType)menu_id);
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