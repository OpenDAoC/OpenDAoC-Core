namespace DOL.GS.Commands;

[Command("&dropmr", //command to handle
    ePrivLevel.Player, //minimum privelege level
    "Drops the Minotaurrelic.", //command description
    "/dropmr")] //usage
public class DropMrCommand : ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (client.Player.MinotaurRelic == null) return;

        client.Player.MinotaurRelic.PlayerLoosesRelic(client.Player, false);
    }
}