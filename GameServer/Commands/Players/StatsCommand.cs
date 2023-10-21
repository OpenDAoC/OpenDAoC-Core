namespace Core.GS.Commands;

[Command(
	"&stats",
	EPrivLevel.Player,
	"Displays player statistics")]

public class StatsCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "stats"))
			return;

		if (client == null) return;

        if (args.Length > 1)
        {
            string playerName = "";

            if (args[1].ToLower() == "player")
            {
                if (args.Length > 2)
                {
                    playerName = args[2];
                }
                else
                {
                    // try and get player name from target
                    if (client.Player.TargetObject != null && client.Player.TargetObject is GamePlayer)
                    {
                        playerName = client.Player.TargetObject.Name;
                    }
                }
            }

            client.Player.Statistics.DisplayServerStatistics(client, args[1].ToLower(), playerName);
        }
        else
        {
            DisplayMessage(client, client.Player.Statistics.GetStatisticsMessage());
        }
	}
}