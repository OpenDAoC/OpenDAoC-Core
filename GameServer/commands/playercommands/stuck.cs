/*namespace DOL.GS.Commands
{
    [CmdAttribute("&stuck",
        ePrivLevel.Player, //minimum privelege level
        "Removes the player from the world and put it to a safe location", //command description
        "/stuck")] //usage
    public class StuckCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "stuck"))
                return;
        }
    }
}*/
