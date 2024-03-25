namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&gmstealth",
        ePrivLevel.GM,
        "Grants the ability to stealth to a gm/admin character",
        "/gmstealth on : turns the command on",
        "/gmstealth off : turns the command off")]
    public class GMStealthCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (args.Length != 2)
                DisplaySyntax(client);
            else if (args[1].Equals("on", System.StringComparison.OrdinalIgnoreCase))
                client.Player.Stealth(true);
            else if (args[1].Equals("off", System.StringComparison.OrdinalIgnoreCase))
                client.Player.Stealth(false);
        }
    }
}
