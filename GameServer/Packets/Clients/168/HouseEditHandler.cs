using System.Collections.Generic;
using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.HouseEdit, "Change handler for outside/inside look (houses).", EClientStatus.PlayerInGame)]
public class HouseEditHandler : IPacketHandler
{
	private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		packet.ReadShort(); // playerId no use for that.

		// house is null, return
		var house = client.Player.CurrentHouse;
		if(house == null)
			return;

		// grab all valid changes
		var changes = new List<int>();
		for (int i = 0; i < 10; i++)
		{
			int swtch = packet.ReadByte();
			int change = packet.ReadByte();

			if (swtch != 255)
				changes.Add(change);
		}

		// apply changes
		if (changes.Count > 0)
			house.Edit(client.Player, changes);
	}
}