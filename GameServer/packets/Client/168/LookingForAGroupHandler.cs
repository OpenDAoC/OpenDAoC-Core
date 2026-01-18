using System.Collections;

namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.LookingForGroup, "handle Looking for a group", eClientStatus.PlayerInGame)]
	public class LookingForAGroupHandler : PacketHandler
	{
		//rewritten by Corillian so if it doesn't work you know who to yell at ;)
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			byte grouped = (byte)packet.ReadByte();
			ArrayList list = new ArrayList();
			if (grouped != 0x00)
			{
				var groups = GroupMgr.ListGroupByStatus(0x00);
				if (groups != null)
				{
					foreach (Group group in groups)
						if (GameServer.ServerRules.IsAllowedToGroup(group.Leader, client.Player, true))
						{
							list.Add(group.Leader);
						}
				}
			}

			var Lfg = GroupMgr.LookingForGroupPlayers();

			if (Lfg != null)
			{
				foreach (GamePlayer player in Lfg)
				{
					if (player != client.Player && GameServer.ServerRules.IsAllowedToGroup(client.Player, player, true))
					{
						list.Add(player);
					}
				}
			}

			client.Out.SendFindGroupWindowUpdate((GamePlayer[])list.ToArray(typeof(GamePlayer)));
		}
	}
}