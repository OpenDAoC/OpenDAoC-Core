using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using DOL.GS.ServerProperties;
using DOL.Logging;
using JNogueira.Discord.WebhookClient;

namespace DOL.GS
{
    public static class DiscordClientManager
    {
        private static readonly Logger log = LoggerManager.Create(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Dictionary<WebhookType, DiscordWebhookClient> _clients = new();

        public static void Initialize()
        {
            if (!Properties.DISCORD_ACTIVE)
                return;

            TryAddClient(WebhookType.Default, Properties.DISCORD_WEBHOOK_ID);
            TryAddClient(WebhookType.RvR, Properties.DISCORD_RVR_WEBHOOK_ID);
            TryAddClient(WebhookType.AlbChat, Properties.DISCORD_ALBCHAT_WEBHOOK_ID);
            TryAddClient(WebhookType.MidChat, Properties.DISCORD_MIDCHAT_WEBHOOK_ID);
            TryAddClient(WebhookType.HibChat, Properties.DISCORD_HIBCHAT_WEBHOOK_ID);
        }

        public static bool TryGetClient(WebhookType type, out DiscordWebhookClient client)
        {
            client = null;
            return Properties.DISCORD_ACTIVE && _clients.TryGetValue(type, out client);
        }

        private static void TryAddClient(WebhookType type, string webhookUrl)
        {
            if (string.IsNullOrEmpty(webhookUrl))
            {
                if (log.IsWarnEnabled)
                    log.Warn($"Discord webhook URL for {type} is not set. Skipping client creation.");

                return;
            }

            try
            {
                HttpClient httpClient = new()
                {
                    BaseAddress = new Uri(webhookUrl),
                    Timeout = TimeSpan.FromSeconds(30)
                };

                DiscordWebhookHttpClient webhookHttpClient = new(httpClient);
                DiscordWebhookClient discordClient = new(webhookHttpClient);
                _clients[type] = discordClient;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to create Discord client for {type} with URL {webhookUrl}: {ex.Message}");
            }
        }
    }

    public enum WebhookType
    {
        Default,
        RvR,
        AlbChat,
        HibChat,
        MidChat
    }
}
