using Core.GS.Enums;
using Core.GS.Expansions.Foundations;
using Core.Language;

namespace Core.GS.Commands;

[Command(
  "&boot",
  EPrivLevel.Player,
   "Kicks a player out of your house",
	 "Useage: /boot [playername]")]
public class BootCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "boot"))
			return;

        House house = client.Player.CurrentHouse;
		if (house == null)
		{
            DisplayMessage(client, LanguageMgr.GetTranslation(client.Account.Language, "Scripts.Players.Boot.InHouseError"));
			return;
		}

		// no permission to banish, return
		if (!house.CanBanish(client.Player))
		{
			DisplayMessage(client, "You do not have permissions to do that.");
			return;
		}

		// check each player, try and find player with the given name (lowercase cmp)
		foreach (GamePlayer player in house.GetAllPlayersInHouse())
		{
			if (player != client.Player && player.Name.ToLower() != args[1].ToLower())
			{
				ChatUtil.SendSystemMessage(client, "Scripts.Players.Boot.YouRemoved", client.Player.Name);
				player.LeaveHouse();

				return;
			}
		}

		ChatUtil.SendHelpMessage(client, "Scripts.Players.Boot.NoOneOnline", null);
	}
}