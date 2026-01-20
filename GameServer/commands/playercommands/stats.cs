using System;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&stats",
        ePrivLevel.Player,
        "Displays player statistics")]
    public class StatsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "stats"))
                return;

            if (args.Length > 1)
            {
                string playerName = string.Empty;

                if (args[1].Equals("player", StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > 2)
                        playerName = args[2];
                    else if (client.Player.TargetObject is GamePlayer)
                        playerName = client.Player.TargetObject.Name;
                }

                client.Player.Statistics.DisplayServerStatistics(client, args[1], playerName);
            }
            else
                DisplayMessage(client, client.Player.Statistics.GetStatisticsMessage());
        }
    }
}