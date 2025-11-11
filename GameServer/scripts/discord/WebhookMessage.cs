using DOL.GS.PacketHandler;
using JNogueira.Discord.WebhookClient;

namespace DOL.GS.Scripts.discord
{
    public static class WebhookMessage
    {
        public static void SendMessage(DiscordWebhookClient discordClient, string message, string avatar = null, string userName = "Server Bot")
        {
            var msg = new DiscordMessage(
                content: message,
                username: userName,
                avatarUrl: avatar ?? string.Empty,
                tts: false
            );

            discordClient.SendToDiscordAsync(msg);
        }

        public static void LogChatMessage(GamePlayer player, eChatType chatType, string message)
        {
            string formattedMessage;

            switch (chatType)
            {
                case eChatType.CT_Broadcast:
                    formattedMessage = "**[REGION - " + player.CurrentZone.Description + "] ";
                    break;
                case eChatType.CT_Help:
                    formattedMessage = "**[HELP] ";
                    break;
                case eChatType.CT_Advise:
                    formattedMessage = "**[ADVICE] ";
                    break;
                case eChatType.CT_LFG:
                    formattedMessage = "**[LFG] (" + player.CharacterClass.Name + " " + player.Level + ") ";
                    break;
                case eChatType.CT_Trade:
                    formattedMessage = "**[TRADE] ";
                    break;
                default:
                    formattedMessage = "**[UNKNOWN] ";
                    break;
            }

            formattedMessage += player.Name + ":** " + message;

            switch (player.Realm)
            {
                case eRealm.Albion:
                {
                    if (DiscordClientManager.TryGetClient(WebhookType.AlbChat, out var discordClient))
                        SendMessage(discordClient, formattedMessage);

                    break;
                }
                case eRealm.Midgard:
                {
                    if (DiscordClientManager.TryGetClient(WebhookType.MidChat, out var discordClient))
                        SendMessage(discordClient, formattedMessage);

                    break;
                }
                case eRealm.Hibernia:
                {
                    if (DiscordClientManager.TryGetClient(WebhookType.HibChat, out var discordClient))
                        SendMessage(discordClient, formattedMessage);

                    break;
                }
            }
        }
    }
}
