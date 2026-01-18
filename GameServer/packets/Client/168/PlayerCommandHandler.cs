namespace DOL.GS.PacketHandler.Client.v168
{
    [PacketHandlerAttribute(PacketHandlerType.TCP, eClientPackets.CommandHandler, "Handles the players commands", eClientStatus.PlayerInGame)]
    public class PlayerCommandHandler : PacketHandler
    {
        private string _lastUsedCommand = string.Empty;

        protected override void HandlePacketInternal(GameClient client, GSPacketIn packet)
        {
            packet.Skip(1);

            if (client.Version < GameClient.eClientVersion.Version1127)
                packet.Skip(7);

            _lastUsedCommand = packet.ReadString(255);

            if (!ScriptMgr.HandleCommand(client, _lastUsedCommand))
            {
                if (_lastUsedCommand.Length > 0 && _lastUsedCommand[0] == '&')
                    _lastUsedCommand = "/" + _lastUsedCommand[1..];

                client.Out.SendMessage($"No such command ({_lastUsedCommand})", eChatType.CT_System, eChatLoc.CL_SystemWindow);
            }
        }

        protected override string GetLogContext()
        {
            return _lastUsedCommand;
        }
    }
}
