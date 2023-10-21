using Core.GS.PacketHandler;
using Core.GS.Quests;

namespace Core.GS.Commands;

[Command(
	"&search",
	EPrivLevel.Player,
	"Search the current area.",
	"/search")]
public class QuestSearchCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "search"))
			return;

		GamePlayer player = client.Player;

		if (player == null)
			return;

		bool searched = false;

		foreach (AQuest quest in player.QuestList.Keys)
		{
			if (quest.Command(player, EQuestCommand.SEARCH))
			{
				searched = true;
			}
		}

        // Also check for DataQuests started via searching

        if (searched == false)
        {
            foreach (AbstractArea area in player.CurrentAreas)
            {
                if (area is QuestSearchArea && (area as QuestSearchArea).DataQuest != null && (area as QuestSearchArea).Step == 0)
                {
                    if ((area as QuestSearchArea).DataQuest.Command(player, EQuestCommand.SEARCH_START, area))
                    {
                        searched = true;
                    }
                }
            }
        }

		if (searched == false)
		{
			player.Out.SendMessage("You can't do that here!", EChatType.CT_Important, EChatLoc.CL_SystemWindow);
		}
	}
}