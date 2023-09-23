using System.Collections.Generic;

namespace DOL.GS.Commands
{
    [Cmd(
        "&clientlist",
        ePrivLevel.GM,
        "Usage: /clientlist [full] - full option includes IP's and accounts",
        "Show a list of currently playing clients and their ID's")]
    public class ClientListCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            List<string> message = new();

            foreach (GamePlayer player in ClientService.GetPlayers())
            {
                if (args.Length > 1 && args[1].Equals("full", System.StringComparison.OrdinalIgnoreCase))
                    message.Add($"({player.Client.SessionID}) {player.Client.TcpEndpointAddress}, {player.Client.Account.Name}, {player.Name}, {player.Client.Version}");
                else
                    message.Add($"({player.Client.SessionID}) {player.Name}");
            }

            client.Out.SendCustomTextWindow("[Playing Client List]", message);
            return;
        }
    }
}
