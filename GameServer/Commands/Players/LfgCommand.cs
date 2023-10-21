using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;
using Core.GS.Scripts.discord;
using Core.GS.ServerProperties;
using Core.Language;

namespace Core.GS.Commands;

[Command(
     "&lfg",
     EPrivLevel.Player,
     "Broadcast a LFG message to other players in the same region",
     "/lfg <message>")]
public class LfgCommand : ACommandHandler, ICommandHandler
{
    private const string LFG_TIMEOUT_KEY = "lastLFGTick";

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

        string message = string.Join(" ", args, 1, args.Length - 1);
        long lastLfgTick = client.Player.TempProperties.GetProperty<long>(LFG_TIMEOUT_KEY);
        int slowModeLength = Properties.LFG_SLOWMODE_LENGTH * 1000;

        if ((GameLoopMgr.GameLoopTime - lastLfgTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
        {
            // Message: You must wait {0} seconds before using this command again.
            ChatUtil.SendSystemMessage(client, "PLCommands.LFG.List.Wait", Properties.LFG_SLOWMODE_LENGTH - (GameLoopMgr.GameLoopTime - lastLfgTick) / 1000);
            return;
        }

        Broadcast(client.Player, message);
    }

    private static void Broadcast(GamePlayer player, string message)
    {
        foreach (GamePlayer otherPlayer in ClientService.GetPlayersForRealmWideChatMessage(player))
            otherPlayer.Out.SendMessage(LanguageMgr.GetTranslation(player.Client.Account.Language, "Scripts.Players.LFG.Message", $"{player.Name} ({player.Level}, {player.PlayerClass.Name})", message), EChatType.CT_LFG, EChatLoc.CL_ChatWindow);

        if (Properties.DISCORD_ACTIVE)
            WebhookMessage.LogChatMessage(player, EChatType.CT_LFG, message);

        if (player.Client.Account.PrivLevel == 1)
            player.Client.Player.TempProperties.SetProperty(LFG_TIMEOUT_KEY, GameLoopMgr.GameLoopTime);
    }
}