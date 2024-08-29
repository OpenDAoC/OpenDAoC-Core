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
 */

using System.Collections.Generic;
using System.Linq;
using DOL.GS.Quests;

namespace DOL.GS.Commands
{
    [CmdAttribute(
        "&quest",
        new string[] {"&quests"},
        ePrivLevel.Player,
        "Display a list of your ongoing and completed quests", "/quest")]
    public class QuestCommandHandler : AbstractCommandHandler, ICommandHandler
    {
        public void OnCommand(GameClient client, string[] args)
        {
            if (IsSpammingCommand(client.Player, "quest"))
                return;

            string message = string.Empty;
            List<AbstractQuest> activeQuests = client.Player.QuestList.Keys.ToList();
            List<AbstractQuest> finishedQuests = client.Player.GetFinishedQuests();

            if (activeQuests.Count == 0)
                message += "You have no pending quests currently.";
            else
            {
                if (activeQuests.Count < 10)
                {
                    message += $"You are currently working on {activeQuests.Count} quest(s), including:";

                    foreach (AbstractQuest quest in activeQuests)
                    {
                        // Need to protect from too long a list
                        // We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
                        if (message.Length < 1500)
                            message += $"\n{quest.Name}";
                        else
                            message += string.Empty;
                    }
                }
                else
                    message += $"You are currently working on {activeQuests.Count} quests.";
            }

            if (finishedQuests.Count == 0)
                message += "\nYou have not yet completed any quests.";
            else
            {
                if (finishedQuests.Count < 10)
                {
                    message += "\nYou have completed the following quest(s):";

                    foreach (AbstractQuest quest in finishedQuests)
                    {
                        // Need to protect from too long a list
                        // We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
                        if (message.Length < 1500)
                            message += string.Format("\n{0}", quest.Name);
                        else
                            message += string.Empty;
                    }
                }
                else
                    message += $"\nYou have completed {finishedQuests.Count} quests.";
            }

            message += "\nUse the '/journal' command to view your full ongoing and completed quests.";
            DisplayMessage(client, message);
        }
    }
}
