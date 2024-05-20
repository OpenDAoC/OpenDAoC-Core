namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CreateNPCRequest, "Handles requests for npcs(0x72) in game", eClientStatus.PlayerInGame)]
	public class NPCCreationRequestHandler : IPacketHandler
	{
		public void HandlePacket(GameClient client, GSPacketIn packet)
		{
			if (client.Player == null)
				return;
			Region region = client.Player.CurrentRegion;
			if (region == null)
				return;

			ushort id;
			if (client.Version >= GameClient.eClientVersion.Version1126)
				id = packet.ReadShortLowEndian(); // Dre: disassembled game.dll show a write of uint, is it a wip in the game.dll?
			else
				id = packet.ReadShort();
			GameNPC npc = region.GetObject(id) as GameNPC;
			if (npc == null || !client.Player.IsWithinRadius(npc, WorldMgr.OBJ_UPDATE_DISTANCE))
			{
				client.Out.SendObjectDelete(id);
				return;
			}

			if (npc != null)
				ClientService.CreateNpcForPlayer(client.Player, npc);
		}
	}
}
