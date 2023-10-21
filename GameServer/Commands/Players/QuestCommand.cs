using System.Collections.Generic;
using System.Linq;
using DOL.GS.Quests;

namespace DOL.GS.Commands;

[Command(
    "&quest",
    new string[] {"&quests"},
    EPrivLevel.Player,
    "Display a list of your ongoing and completed quests", "/quest")]
public class QuestCommand : ACommandHandler, ICommandHandler
{
    public void OnCommand(GameClient client, string[] args)
    {
        if (IsSpammingCommand(client.Player, "quest"))
            return;

        string message = "";
        List<AQuest> activeQuests = client.Player.QuestList.Keys.ToList();
        List<AQuest> finishedQuests = client.Player.GetFinishedQuests();

        if (activeQuests.Count == 0)
            message += "You have no pending quests currently.";
        else
        {
            if (activeQuests.Count < 10)
            {
                message += $"You are currently working on {activeQuests.Count} quest(s), including:";

                foreach (AQuest quest in activeQuests)
                {
                    // Need to protect from too long a list
                    // We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
                    if (message.Length < 1500)
                        message += $"\n{quest.Name}";
                    else
                        message += "";
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

                foreach (AQuest quest in finishedQuests)
                {
                    // Need to protect from too long a list
                    // We'll do an easy sloppy chop at 1500 characters (packet limit is 2048)
                    if (message.Length < 1500)
                        message += string.Format("\n{0}", quest.Name);
                    else
                        message += "";
                }
            }
            else
                message += $"\nYou have completed {finishedQuests.Count} quests.";
        }

        message += "\nUse the '/journal' command to view your full ongoing and completed quests.";
        DisplayMessage(client, message);
    }
}