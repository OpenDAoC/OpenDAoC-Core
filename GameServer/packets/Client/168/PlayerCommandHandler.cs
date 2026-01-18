namespace DOL.GS.PacketHandler.Client.v168
{
	[PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CommandHandler, "Handles the players commands", eClientStatus.PlayerInGame)]
	public class PlayerCommandHandler : PacketHandler
	{
		protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
		{
			packet.Skip(1);
			if (client.Version < GameClient.eClientVersion.Version1127)
				packet.Skip(7);
			string cmdLine = packet.ReadString(255);
			if(!ScriptMgr.HandleCommand(client, cmdLine))
			{
				if (cmdLine[0] == '&')
					cmdLine = "/" + cmdLine.Remove(0, 1);
				client.Out.SendMessage($"No such command ({cmdLine})", eChatType.CT_System,eChatLoc.CL_SystemWindow);
			}
		}
	}
}