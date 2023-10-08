using DOL.GS.Quests;

namespace DOL.GS.Commands;

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

		foreach (AbstractQuest quest in player.QuestList.Keys)
		{
			if (quest.Command(player, AbstractQuest.eQuestCommand.SEARCH))
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
                    if ((area as QuestSearchArea).DataQuest.Command(player, AbstractQuest.eQuestCommand.SEARCH_START, area))
                    {
                        searched = true;
                    }
                }
            }
        }

		if (searched == false)
		{
			player.Out.SendMessage("You can't do that here!", DOL.GS.PacketHandler.EChatType.CT_Important, DOL.GS.PacketHandler.EChatLoc.CL_SystemWindow);
		}
	}
}