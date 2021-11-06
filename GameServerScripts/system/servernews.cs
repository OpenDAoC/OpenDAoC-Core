/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 * Author: clait
 * Server: Atlas Freeshard
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using DOL.GS.PacketHandler;
using DOL.Language;

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

        public class ClassToCount
        {
            public string name;
            public int count;

            public ClassToCount(string name, int count)
            {
                this.name = name;
                this.count = count;
            }
        }

        private List<ClassToCount> classcount = new List<ClassToCount>();

        public void OnCommand(GameClient client, string[] args)
        {
            DateTime thisDay = DateTime.Today;
            
            var news = new List<string>();
            private readonly HttpClient _httpClient;
            public MyService(HttpClient httpClient) {
                _httpClient = httpClient;
            }
   
            public async Task getNews() {
                var latestNews = await _httpClient.GetStringAsync("https://admin.atlasfreeshard.com/storage/servernews.txt");
                
                foreach (var line in latestNews)
                {
                    news.Add(line);
                };
            }
            
            client.Out.SendCustomTextWindow("Server News " + thisDay.ToString("d"), news);
            client.Out.SendCustomDialog();
            
            return;

        }
    }
}