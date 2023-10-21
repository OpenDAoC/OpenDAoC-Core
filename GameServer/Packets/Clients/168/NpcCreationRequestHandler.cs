using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CreateNPCRequest, "Handles requests for npcs(0x72) in game", EClientStatus.PlayerInGame)]
public class NpcCreationRequestHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
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
		GameNpc npc = region.GetObject(id) as GameNpc;
		if (npc == null || !client.Player.IsWithinRadius(npc, WorldMgr.OBJ_UPDATE_DISTANCE))
		{
			client.Out.SendObjectDelete(id);
			return;
		}

		if (npc != null)
		{
			client.Out.SendNPCCreate(npc);
			
			if (npc.Inventory != null)
				client.Out.SendLivingEquipmentUpdate(npc);
			
			//DO NOT SEND A NPC UPDATE, it is done in Create anyway
			//Sending a Update causes a UDP packet to be sent and
			//the client will get the UDP packet before the TCP Create packet
			//Causing the client to issue another NPC CREATION REQUEST!
			//client.Out.SendNPCUpdate(npc); <-- BIG NO NO
		}
	}
}