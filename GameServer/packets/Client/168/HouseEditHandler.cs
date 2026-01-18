using System.Collections.Generic;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.HouseEdit, "Change handler for outside/inside look (houses).", eClientStatus.PlayerInGame)]
	public class HouseEditHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
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
}