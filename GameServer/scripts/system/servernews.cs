using System;
using System.Collections.Generic;
using System.Net.Http;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&servernews",
         new string[] { "&sn" },
        ePrivLevel.Player,
        "Shows the current Server News",
        "Usage: /servernews")]
    public class ServerNewsCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            var today = DateTime.Today;

            using var newsClient = new HttpClient();
            string newsTxt;
            var news = new List<string>();
            const string url = "https://admin.atlasfreeshard.com/storage/servernews.txt";
            newsTxt = newsClient.GetStringAsync(url).Result;
            news.Add(newsTxt);
            client.Out.SendCustomTextWindow("Server News " + today.ToString("d"), news);
        }
    }
}