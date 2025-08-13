namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&bind",
        ePrivLevel.Player,
        "Binds your soul to a bind location, you will start from there after you die and /release",
        "/bind")]
    public class BindCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "bind"))
                return;

            client.Player.Bind();
        }
    }
}
