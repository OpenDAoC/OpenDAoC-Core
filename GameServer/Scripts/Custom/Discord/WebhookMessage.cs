using System;
using DOL.GS.PacketHandler;
using DOL.GS.ServerProperties;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS.Scripts.discord
{
    public static class WebhookMessage
    {
        public static void SendMessage(String webhookId, String message, String userName = "Server Bot", string avatar = "")
        {
            var client = new DiscordWebhookClient(webhookId);

            var content = message;
            var msg = new DiscordMessage(
                content,
                username: userName,
                avatarUrl: avatar,
                tts: false
            );
            client.SendToDiscord(msg);
        }
        public static void SendEmbeddedMessage(String webhookId, String message)
        {
            //TODO: Possibly implement this to have all other discord messages delegate through something like this
        }
        
        public static void LogChatMessage(GamePlayer player, EChatType chatType, String message)
        {
            // Format message
            String formattedMessage = "";
            switch (chatType)
            {
                case EChatType.CT_Broadcast:
                    formattedMessage = "**[REGION - " + player.CurrentZone.Description + "] ";
                    break;
                case EChatType.CT_Help:
                    formattedMessage = "**[HELP] ";
                    break;
                case EChatType.CT_Advise:
                    formattedMessage = "**[ADVICE] ";
                    break;
                case EChatType.CT_LFG:
                    formattedMessage = "**[LFG] (" + player.CharacterClass.Name + " " + player.Level + ") ";
                    break;
                case EChatType.CT_Trade:
                    formattedMessage = "**[TRADE] ";
                    break;
                default:
                    formattedMessage = "**[UNKNOWN] ";
                    break;
            }
            formattedMessage += player.Name + ":** " + message;

            string avatar;
            
            
            // Send to Discord
            switch (player.Realm)
            {
                case ERealm.Albion:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_ALBCHAT_WEBHOOK_ID))
                    {
                        avatar = "";
                        SendMessage(Properties.DISCORD_ALBCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
                case ERealm.Hibernia:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_HIBCHAT_WEBHOOK_ID))
                    {
                        avatar = "";
                        SendMessage(Properties.DISCORD_HIBCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
                case ERealm.Midgard:
                    if (!string.IsNullOrEmpty(Properties.DISCORD_MIDCHAT_WEBHOOK_ID))
                    {
                        avatar = "";
                        SendMessage(Properties.DISCORD_MIDCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
            }
            
        }
    }
}