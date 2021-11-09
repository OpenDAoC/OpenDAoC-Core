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

            //news = "18/06/2021\n- Website launched at www.atlasfreeshard.com\n\n17/06/2021\n- Added Stygia Haven as teleport location for all realms as end-game farming zone\n- Various teleports, hastener and trainer improvements\n\n16/06/2021\n - Hibernia Classic cities now all have basic NPCs in place (teleporter, smith, healer, master trainer)\n - Implemented Atlas Orbs (xp items)".Split('\n');

            using (WebClient newsClient = new WebClient())
            {
                string newsTxt;
                var news = new List<string>();
                string url = "https://admin.atlasfreeshard.com/storage/servernews.txt";
                newsTxt = newsClient.DownloadString(url);
                news.Add(newsTxt);
                client.Out.SendCustomTextWindow("Server News " + thisDay.ToString("d"), news);
            }
            return;

        }
    }
}