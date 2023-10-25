using Core.GS.Enums;

namespace Core.GS.Commands;

[Command("&sit", new string[] { "&rest" }, EPrivLevel.Player, "Sit", "/sit")]
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