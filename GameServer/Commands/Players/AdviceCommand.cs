using System;
using Core.GS.ECS;
using Core.GS.Enums;
using Core.GS.GameLoop;
using Core.GS.PacketHandler;
using Core.GS.Scripts.discord;
using Core.GS.ServerProperties;

namespace Core.GS.Commands;

[Command(
    "&advice",
     new [] { "&adv" },
    EPrivLevel.Player,
    // Displays next to the command when '/cmd' is entered
    "Lists all flagged Advisors, sends advisors questions, and sends messages to the Advice channel.",
    // Message: '/adv <message>' - Sends a message to the Advice channel.
    "PLCommands.Advice.Syntax.AdvChannel",
    // Message: '/advice' - Lists all online Advisors.
    "PLCommands.Advice.Syntax.Advice",
    // '/advisor' - Flags your character as an Advisor (<ADV>) to indicate that you are willing to answer new players' questions.
    "PLCommands.Advisor.Syntax.Advisor",
    // Message: '/advisor <advisorName> <message>' - Directly messages an Advisor with your question.
    "PLCommands.Advice.Syntax.SendAdvisor")]
public class AdviceCommand : ACommandHandler, ICommandHandler
{
    private const string advTimeoutString = "lastAdviceTick";

    public void OnCommand(GameClient client, string[] args)
    {
        if (client.Player.IsMuted)
        {
            // Message: You have been muted and are not allowed to speak in this channel.
            ChatUtil.SendGMMessage(client, "GMCommands.Mute.Err.NoSpeakChannel", null);
            return;
        }

        if (IsSpammingCommand(client.Player, "advice") || IsSpammingCommand(client.Player, "adv"))
            return;

        long lastAdviceTick = client.Player.TempProperties.GetProperty<long>(advTimeoutString);
        int slowModeLength = Properties.ADVICE_SLOWMODE_LENGTH * 1000;

        if ((GameLoopMgr.GameLoopTime - lastAdviceTick) < slowModeLength && client.Account.PrivLevel == 1) // 60 secs
        {
            // Message: You must wait {0} seconds before using this command again.
            ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Wait", Properties.ADVICE_SLOWMODE_LENGTH - (GameLoopMgr.GameLoopTime - lastAdviceTick) / 1000);
            return;
        }

        string msg = "";

        if (args.Length >= 2)
        {
            for (int i = 1; i < args.Length; ++i)
                msg += $"{args[i]} ";
        }

        if (args.Length == 1)
        {
            int total = 0;
            TimeSpan showPlayed = TimeSpan.FromSeconds(client.Player.PlayedTime);

            // Message: The following players are flagged as Advisors:
            ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.TheFollowing", null);

            foreach (GamePlayer otherPlayer in ClientService.GetPlayers(Predicate, (client.Player.Realm, client.Account.PrivLevel > 1)))
            {
                total++;

                if (!otherPlayer.ClassNameFlag && otherPlayer.CraftTitle.GetValue(otherPlayer, client.Player).StartsWith("Legendary"))
                {
                    // Message: {0}) {1}, Level {2} {3} ({4} days, {5} hours, {6} minutes played)
                    // Example: 1) Fen, Level 43 Legendary Grandmaster Basic Crafter (1 days, 3 hours, 31 minutes played)
                    ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Result", total, otherPlayer.Name, otherPlayer.Level, otherPlayer.CraftTitle.GetValue(otherPlayer, client.Player), showPlayed.Days, showPlayed.Hours, showPlayed.Minutes);
                }
                else
                    // Message: "{0}) {1}, Level {2} {3} ({4} days, {5} hours, {6} minutes played)"
                    // Example: 1) Kelt, Level 50 Bard (14 days, 3 hours, 31 minutes played)
                    ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.Result", total, otherPlayer.Name, otherPlayer.Level, otherPlayer.PlayerClass.Name, showPlayed.Days, showPlayed.Hours, showPlayed.Minutes);
            }

            if (total == 1)
                // Message: There is 1 Advisor online!
                ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.1AdvisorOn", null);
            else
                // Message: There are {0} Advisors online!
                ChatUtil.SendSystemMessage(client, "PLCommands.Advice.List.AdvisorsOn", total);

            return;
        }

        foreach (GamePlayer otherPlayer in ClientService.GetPlayersForRealmWideChatMessage(client.Player))
        {
            // Message: [ADVICE {0}] {1}: {2}
            ChatUtil.SendAdviceMessage(otherPlayer, "Social.SendAdvice.Msg.Channel", GetRealmString(client.Player.Realm), client.Player.Name, msg);
        }

        if (Properties.DISCORD_ACTIVE)
            WebhookMessage.LogChatMessage(client.Player, EChatType.CT_Advise, msg);

        if (client.Account.PrivLevel == 1)
            client.Player.TempProperties.SetProperty(advTimeoutString, GameLoopMgr.GameLoopTime);

        static bool Predicate(GamePlayer player, (ERealm realm, bool isGm) args)
        {
            return player.Advisor && ((player.Realm == args.realm && !player.IsAnonymous) || args.isGm);
        }
    }

    public string GetRealmString(ERealm Realm)
    {
        return Realm switch
        {
            ERealm.Albion => "ALB",
            ERealm.Midgard => "MID",
            ERealm.Hibernia => "HIB",
            _ => "NONE",
        };
    }
}