namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CommandHandler, "Handles the players commands", eClientStatus.PlayerInGame)]
    public class PlayerCommandHandler : PacketHandler
    {
        private const string LAST_USED_COMMAND_KEY = "LAST_USED_COMMAND_KEY";

        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            packet.Skip(1);

            if (client.Version < GameClient.eClientVersion.Version1127)
                packet.Skip(7);

            string command = packet.ReadString(255);
            client.Player.TempProperties.SetProperty(LAST_USED_COMMAND_KEY, command);

            if (!ScriptMgr.HandleCommand(client, command))
            {
                if (command.Length > 0 && command[0] == '&')
                    command = "/" + command[1..];

                client.Out.SendMessage($"No such command ({command})", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        protected override string GetLogContext(GameClient client, GSPacketIn packet)
        {
            return client.Player.TempProperties.GetProperty<string>(LAST_USED_COMMAND_KEY);
        }
    }
}
