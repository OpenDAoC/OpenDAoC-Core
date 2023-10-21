using Core.GS.PacketHandler;
using Core.GS.Scripts.discord;
using Core.GS.ServerProperties;
using Core.Language;

namespace Core.GS.Commands;

[Command(
     "&trade",
     EPrivLevel.Player,
     "Broadcast a trade message to other players in the same region",
     "/trade <message>")]
public class TradeChannelCommand : ACommandHandler, ICommandHandler
{
    private const string tradeTimeoutString = "lastTradeTick";

    public void OnCommand(GameClient client, string[] args)
    {
        if (args.Length < 2)
        {
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Broadcast.NoText"));
            return;
        }

        if (client.Player.IsMuted)
        {
            client.Player.Out.SendMessage("You have been muted. You cannot broadcast.", EChatType.CT_Staff, EChatLoc.CL_SystemWindow);
            return;
        }

        long lastTradeTick = client.Player.TempProperties.GetProperty<long>(tradeTimeoutString);
        int slowModeLength = Properties.TRADE_SLOWMODE_LENGTH * 1000;

        if ((GameLoop.GameLoopTime - lastTradeTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
        {
            // Message: You must wait {0} seconds before using this command again.
            ChatUtil.SendSystemMessage(client, "PLCommands.Trade.List.Wait", Properties.TRADE_SLOWMODE_LENGTH - (GameLoop.GameLoopTime - lastTradeTick) / 1000);
            return;
        }

        string message = string.Join(" ", args, 1, args.Length - 1);
        Broadcast(client.Player, message);
    }

    private static void Broadcast(GamePlayer player, string message)
    {
        foreach (GamePlayer otherPlayer in ClientService.GetPlayersForRealmWideChatMessage(player))
            otherPlayer.Out.SendMessage($"[Trade] {player.Name}: {message}", EChatType.CT_Trade, EChatLoc.CL_ChatWindow);

        if (Properties.DISCORD_ACTIVE)
            WebhookMessage.LogChatMessage(player, EChatType.CT_Trade, message);

        if (player.Client.Account.PrivLevel == 1)
            player.Client.Player.TempProperties.SetProperty(tradeTimeoutString, GameLoop.GameLoopTime);
    }
}