using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&spacing",
	EPrivLevel.Player,
	"Change the spacing of your pets!", "/spacing {normal, big, huge}")]
public class SpacingCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "spacing"))
			return;

		GamePlayer player = client.Player;

		//No one else needs to use this spell
		if (player.PlayerClass.ID != (int)EPlayerClass.Bonedancer)
		{
			DisplayMessage(player, "Only Bonedancers can use this command!");
			return;
		}

		//Help display
		if (args.Length == 1)
		{
			DisplayMessage(player, "Spacing commands:");
			DisplayMessage(player, "'/spacing normal' Use normal spacing between minions.");
			DisplayMessage(player, "'/spacing big' Use a larger spacing between minions.");
			DisplayMessage(player, "'/spacing huge' Use a very large spacing between minions.");
			return;
		}

		//Check to see if the BD has a commander and minions
		if (player.ControlledBrain == null)
		{
			DisplayMessage(player, "You don't have a commander!");
			return;
		}
		bool haveminion = false;
		foreach (IControlledBrain icb in player.ControlledBrain.Body.ControlledNpcList)
		{
			if (icb != null)
				haveminion = true;
		}
		if (!haveminion)
		{
			DisplayMessage(player, "You don't have any minions!");
			return;
		}

		switch (args[1].ToLower())
		{
			//Triangle Formation
			case "normal":
				player.ControlledBrain.Body.FormationSpacing = 1;
				break;
			//Line formation
			case "big":
				player.ControlledBrain.Body.FormationSpacing = 2;
				break;
			//Protect formation
			case "huge":
				player.ControlledBrain.Body.FormationSpacing = 3;
				break;
			default:
				DisplayMessage(player, "Unrecognized argument: " + args[1]);
				break;
		}
	}
}