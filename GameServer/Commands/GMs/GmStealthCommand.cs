namespace DOL.GS.Commands;

[Command(
    "&gmstealth",
    ePrivLevel.GM,
    "Grants the ability to stealth to a gm/admin character",
    "/gmstealth on : turns the command on",
    "/gmstealth off : turns the command off")]
public class GmStealthCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length != 2) {
        	DisplaySyntax(client);
        }
        else if (args[1].ToLower().Equals("on")) {

            if (client.Player.IsStealthed != true)
            {
               client.Player.Stealth(true);
               client.Player.CurrentSpeed = 191;
            }
        }
        else if (args[1].ToLower().Equals("off"))
        {
                client.Player.Stealth(false);
        }
    }
}