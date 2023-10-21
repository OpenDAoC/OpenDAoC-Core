using Core.GS.ECS;
using Core.GS.PacketHandler;
using Core.GS.Scripts.discord;
using Core.GS.ServerProperties;

namespace Core.GS.Commands;

[Command(
    "&adviceteam",
     new [] { "&advt" },
    EPrivLevel.GM,
    // Displays next to the command when '/cmd' is entered
    "Lists all flagged Advisors, sends advisors questions, and sends messages to the Advice channel as STAFF.")]
public class AdviceTeamCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "adviceteam"))
            return;

        string msg = "";

        if (args.Length >= 2)
        {
            for (int i = 1; i < args.Length; ++i)
                msg += $"{args[i]} ";
        }

        foreach (GamePlayer otherPlayer in ClientService.GetPlayersForRealmWideChatMessage(client.Player))
        {
            var name = "STAFF";
            // Message: [ADVICE {0}] {1}: {2}
            ChatUtil.SendAdviceMessage(otherPlayer, "Social.SendAdvice.Msg.Channel", GetRealmString(client.Player.Realm), name, msg);
        }

        if (Properties.DISCORD_ACTIVE)
            WebhookMessage.LogChatMessage(client.Player, EChatType.CT_Advise, msg);
    }

    private static string GetRealmString(ERealm Realm)
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