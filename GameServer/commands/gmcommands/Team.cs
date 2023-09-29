namespace DOL.GS.Commands
{
    // See the comments above 'using' about SendMessage translation IDs
    [CmdAttribute( 
    // Enter '/team' to see command syntax messages
    "&team",
    new [] { "&te" },
    // Message: <----- '/team' Commands (plvl 2) ----->
    "GMCommands.Header.Command.Team",
    ePrivLevel.GM,
    "Broadcasts a message to all server team members (i.e., plvl 2+).",
    // Syntax: '/team <message>' or '/te <message>'
    "GMCommands.Team.Syntax.Team",
    // Message: Broadcasts a message to all staff members (i.e., plvl 2+).
    "GMCommands.Team.Usage.Team")]

    public class TeamCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            // Lists all '/team' command syntax
            if (args.Length < 2)
            {
                // Message: <----- '/team' Commands (plvl 2) ----->
                ChatUtil.SendHeaderMessage(client, "GMCommands.Header.Command.Team", null);
                // Message: Use the following syntax for this command:
                ChatUtil.SendCommMessage(client, "AllCommands.Command.SyntaxDesc", null);
                // Syntax: '/team <message>' or '/te <message>'
                ChatUtil.SendSyntaxMessage(client, "GMCommands.Team.Syntax.Team", null);
                // Message: Broadcasts a message to all staff members (i.e., plvl 2+).
                ChatUtil.SendCommMessage(client, "GMCommands.Team.Usage.Team", null);	
                return;
            }

            // Identify message body
            string message = string.Join(" ", args, 1, args.Length - 1);

            foreach (GamePlayer otherPlayer in ClientService.GetGmPlayers())
            {
                // Message: [TEAM] {0}: {1}
                ChatUtil.SendTeamMessage(otherPlayer.Client, "Social.ReceiveMessage.Staff.Channel", client.Player.Name, message);
            }
        }
    }
}
