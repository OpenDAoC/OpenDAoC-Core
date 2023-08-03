using System;
using DOL.GS.PacketHandler;
using DOL.GS.PacketHandler.Client.v168;
using DOL.GS.ServerProperties;
using JNogueira.Discord.Webhook.Client;

namespace DOL.GS.Scripts.discord
{
    public static class WebhookMessage
    {
        public static void SendMessage(String webhookId, String message, String userName = "Server Bot", string avatar = "https://cdn.discordapp.com/avatars/924819091028586546/656e2b335e60cb1bfaf3316d7754a8fd.webp")
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
                    if (!string.IsNullOrEmpty(ServerProperties.ServerProperties.DISCORD_ALBCHAT_WEBHOOK_ID))
                    {
                        avatar = "https://cdn.discordapp.com/attachments/861979059550421023/929455017902104627/alb.png";
                        SendMessage(ServerProperties.ServerProperties.DISCORD_ALBCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
                case ERealm.Hibernia:
                    if (!string.IsNullOrEmpty(ServerProperties.ServerProperties.DISCORD_HIBCHAT_WEBHOOK_ID))
                    {
                        avatar = "https://cdn.discordapp.com/attachments/861979059550421023/929455017457496214/hib.png";
                        SendMessage(ServerProperties.ServerProperties.DISCORD_HIBCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
                case ERealm.Midgard:
                    if (!string.IsNullOrEmpty(ServerProperties.ServerProperties.DISCORD_MIDCHAT_WEBHOOK_ID))
                    {
                        avatar = "https://cdn.discordapp.com/attachments/861979059550421023/929455017675616288/mid.png";
                        SendMessage(ServerProperties.ServerProperties.DISCORD_MIDCHAT_WEBHOOK_ID,formattedMessage, avatar: avatar);
                    }
                    break;
            }
            
        }
    }
}