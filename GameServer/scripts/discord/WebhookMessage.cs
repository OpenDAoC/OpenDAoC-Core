using System;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.ServerProperties;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS.Scripts.discord
{
    public static class WebhookMessage
    {
        public static void SendMessage(String webhookId, String message, String userName = "Atlas Bot")
        {
            var client = new DiscordWebhookClient(webhookId);

            var content = message;
            var msg = new DiscordMessage(
                content,
                username: userName,
                avatarUrl: "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp",
                tts: false
            );
            client.SendToDiscord(msg);
        }
        
        public static void SendEmbeddedMessage(String webhookId, String message)
        {
            //TODO: Possibly implement this to have all other discord messages delegate through something like this
        }
        
        public static void LogChatMessage(GamePlayer player, eChatType chatType, String message)
        {
            // Format message
            String formattedMessage = "";
            switch (chatType)
            {
                case eChatType.CT_Broadcast:
                    formattedMessage = "[REGION] ";
                    break;
                case eChatType.CT_Help:
                    formattedMessage = "[HELP] ";
                    break;
                case eChatType.CT_Advise:
                    formattedMessage = "[ADVICE] ";
                    break;
                case eChatType.CT_LFG:
                    formattedMessage = "[LFG] ";
                    break;
                case eChatType.CT_Trade:
                    formattedMessage = "[TRADE] ";
                    break;
                default:
                    formattedMessage = "[UNKNOWN] ";
                    break;
            }
            formattedMessage += player.Name + ": " + message;
            
            // Send to Discord
            switch (player.Realm)
            {
                case eRealm.Albion:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_ALBCHAT_WEBHOOK_ID))
                    {
                        SendMessage(Properties.DISCORD_ALBCHAT_WEBHOOK_ID,formattedMessage);
                    }
                    break;
                case eRealm.Hibernia:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_HIBCHAT_WEBHOOK_ID))
                    {
                        SendMessage(Properties.DISCORD_HIBCHAT_WEBHOOK_ID,formattedMessage);
                    }
                    break;
                case eRealm.Midgard:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_MIDCHAT_WEBHOOK_ID))
                    {
                        SendMessage(Properties.DISCORD_MIDCHAT_WEBHOOK_ID,formattedMessage);
                    }
                    break;
            }
        }
    }
}