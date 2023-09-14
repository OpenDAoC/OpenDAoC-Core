using DOL.GS.PacketHandler;
using DOL.GS.Scripts.discord;
using DOL.GS.ServerProperties;

namespace DOL.GS.Commands
{
    [Cmd(
        "&adviceteam",
         new [] { "&advt" },
        ePrivLevel.GM,
        // Displays next to the command when '/cmd' is entered
        "Lists all flagged Advisors, sends advisors questions, and sends messages to the Advice channel as STAFF.")]
    public class AdviceTeamCommandHandler : AbstractCommandHandler, ICommandHandler
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
                WebhookMessage.LogChatMessage(client.Player, eChatType.CT_Advise, msg);
        }

        private static string GetRealmString(eRealm Realm)
        {
            return Realm switch
            {
                eRealm.Albion => "ALB",
                eRealm.Midgard => "MID",
                eRealm.Hibernia => "HIB",
                _ => "NONE",
            };
        }
    }
}
