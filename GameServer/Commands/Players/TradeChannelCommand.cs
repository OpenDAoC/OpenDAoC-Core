using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.Languages;
using Core.GS.Scripts.discord;
using Core.GS.Server;

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
        int slowModeLength = ServerProperty.TRADE_SLOWMODE_LENGTH * 1000;

        if ((GameLoopMgr.GameLoopTime - lastTradeTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
        {
            // Message: You must wait {0} seconds before using this command again.
            ChatUtil.SendSystemMessage(client, "PLCommands.Trade.List.Wait", ServerProperty.TRADE_SLOWMODE_LENGTH - (GameLoopMgr.GameLoopTime - lastTradeTick) / 1000);
            return;
        }

        string message = string.Join(" ", args, 1, args.Length - 1);
        Broadcast(client.Player, message);
    }

    private static void Broadcast(GamePlayer player, string message)
    {
        foreach (GamePlayer otherPlayer in ClientService.GetPlayersForRealmWideChatMessage(player))
            otherPlayer.Out.SendMessage($"[Trade] {player.Name}: {message}", EChatType.CT_Trade, EChatLoc.CL_ChatWindow);

        if (ServerProperty.DISCORD_ACTIVE)
            WebhookMessage.LogChatMessage(player, EChatType.CT_Trade, message);

        if (player.Client.Account.PrivLevel == 1)
            player.Client.Player.TempProperties.SetProperty(tradeTimeoutString, GameLoopMgr.GameLoopTime);
    }
}