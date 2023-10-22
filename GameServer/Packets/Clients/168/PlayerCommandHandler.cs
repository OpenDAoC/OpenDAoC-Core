using Core.GS.Enums;
using Core.GS.Packets.Server;

namespace Core.GS.Packets.Clients;

[PacketHandler(EPacketHandlerType.TCP, EClientPackets.CommandHandler, "Handles the players commands", EClientStatus.PlayerInGame)]
public class PlayerCommandHandler : IPacketHandler
{
	public void HandlePacket(GameClient client, GsPacketIn packet)
	{
		packet.Skip(1);
		if (client.Version < EClientVersion.Version1127)
			packet.Skip(7);
		string cmdLine = packet.ReadString(255);
		if(!ScriptMgr.HandleCommand(client, cmdLine))
		{
			if (cmdLine[0] == '&')
				cmdLine = "/" + cmdLine.Remove(0, 1);
			client.Out.SendMessage($"No such command ({cmdLine})", EChatType.CT_System,EChatLoc.CL_SystemWindow);
		}
	}
}