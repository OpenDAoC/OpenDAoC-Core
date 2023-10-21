using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseMenuRequest, "Handles housing menu requests", EClientStatus.PlayerInGame)]
public class HousingMenuRequestHandler : IPacketHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	private static Dictionary<int, EMerchantWindowType> _menu168 = new Dictionary<int, EMerchantWindowType>
	{
		{0, EMerchantWindowType.HousingOutsideShop},
		{1, EMerchantWindowType.HousingInsideShop},
		{2, EMerchantWindowType.HousingOutsideMenu},
		{3, EMerchantWindowType.HousingNPCHookpoint},
		{4, EMerchantWindowType.HousingVaultHookpoint},
		{5, EMerchantWindowType.HousingCraftingHookpoint},
		{6, EMerchantWindowType.HousingBindstoneHookpoint},
		{7, (EMerchantWindowType)0xFF}, // not the best but it's ok
		{8, EMerchantWindowType.HousingInsideMenu}, // Interior menu (flag = 0x00 - roof, 0xFF - floor or wall)
	};
	private static Dictionary<int, EMerchantWindowType> _menu1127 = new Dictionary<int, EMerchantWindowType>
	{
		{0, EMerchantWindowType.HousingOutsideShop},
		{1, EMerchantWindowType.HousingInsideShop},
		{2, EMerchantWindowType.HousingDeedMenu},
		{3, EMerchantWindowType.HousingOutsideMenu},
		{4, EMerchantWindowType.HousingNPCHookpoint},
		{5, EMerchantWindowType.HousingVaultHookpoint},
		{6, EMerchantWindowType.HousingCraftingHookpoint},
		{7, EMerchantWindowType.HousingBindstoneHookpoint},
		{8, EMerchantWindowType.HousingInsideMenu},
		{9, (EMerchantWindowType)0xFF}, // not the best but it's ok
	};

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		int housenumber = packet.ReadShort();
		int menuid = packet.ReadByte();
		int flag = packet.ReadByte();

		var house = HouseMgr.GetHouse(client.Player.CurrentRegionID, housenumber);
		if (house == null)
			return;

		if (client.Player == null)
			return;

		client.Player.CurrentHouse = house;

		var menu = _menu168;
		if (client.Version >= GameClient.eClientVersion.Version1127)
			menu = _menu1127;

		if (menu.TryGetValue(menuid, out var type))
			OpenWindow(client, house, type);
		else
			client.Out.SendMessage("Invalid menu id " + menuid + " (hookpoint?).", EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	private void OpenWindow(GameClient client, House house, EMerchantWindowType type)
	{
		switch (type)
		{
			case EMerchantWindowType.HousingOutsideShop:
			case EMerchantWindowType.HousingOutsideMenu:
				if (!house.CanChangeGarden(client.Player, EDecorationPermissions.Add))
					return;
				HouseMgr.SendHousingMerchantWindow(client.Player, type);
				break;

			case EMerchantWindowType.HousingDeedMenu:
			case EMerchantWindowType.HousingVaultHookpoint:
			case EMerchantWindowType.HousingCraftingHookpoint:
			case EMerchantWindowType.HousingBindstoneHookpoint:
			case EMerchantWindowType.HousingNPCHookpoint:
			case EMerchantWindowType.HousingInsideShop:
			case EMerchantWindowType.HousingInsideMenu:
				if (!house.CanChangeInterior(client.Player, EDecorationPermissions.Add))
					return;
				HouseMgr.SendHousingMerchantWindow(client.Player, type);
				break;

			case (EMerchantWindowType)0xFF:
				house.SendHouseInfo(client.Player);
				break;
		}
	}
}