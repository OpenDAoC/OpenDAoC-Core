namespace DOL.GS.Commands;

[Command(
    "&filter",
    EPrivLevel.Player,
    "Turns off the bad word filter.",
    "/filter")]
public class FilterCommand : ACommandHandler, ICommandHandler
{
    /// <summary>
    /// Method to handle the command and any arguments
    /// </summary>
    /// <param name="client"></param>
    /// <param name="args"></param>
    public void OnCommand(GameClient client, string[] args)
    {
        // do nothing, just removes the "command doesn't exist" message.
    }
}