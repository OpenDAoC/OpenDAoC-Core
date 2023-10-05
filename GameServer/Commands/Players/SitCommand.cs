namespace DOL.GS.Commands;

[Command("&sit", new string[] { "&rest" }, ePrivLevel.Player, "Sit", "/sit")]
public class SitCommandHandler : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (!IsSpammingCommand(client.Player, "sitstand"))
        {
            client.Player.Sit(true);
        }
    }
}