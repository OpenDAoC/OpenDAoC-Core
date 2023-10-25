using Core.GS.AI;
using Core.GS.Enums;

namespace Core.GS.Commands;

[Command(
	"&formation",
	EPrivLevel.Player,
	"Change the formation of your pets!", "/formation <type>")]
public class FormationCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "formation"))
			return;

		GamePlayer player = client.Player;

		//No one else needs to use this spell
		if (player.PlayerClass.ID != (int)EPlayerClass.Bonedancer)
		{
			client.Out.SendMessage("Only Bonedancers can use this command!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		//Help display
		if (args.Length == 1)
		{
			client.Out.SendMessage("Formation commands:", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage("'/formation triangle' Place the pets in a triangle formation.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage("'/formation line' Place the pets in a line formation.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			client.Out.SendMessage("'/formation protect' Place the pets in a protect formation that surrounds the commander.", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		//Check to see if the BD has a commander and minions
		if (player.ControlledBrain == null)
		{
			client.Out.SendMessage("You don't have a commander!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}
		bool haveminion = false;
		foreach (IControlledBrain icb in player.ControlledBrain.Body.ControlledNpcList)
		{
			if (icb != null)
			{
				haveminion = true;
				break;
			}
		}
		if (!haveminion)
		{
			client.Out.SendMessage("You don't have any minions!", EChatType.CT_System, EChatLoc.CL_SystemWindow);
			return;
		}

		switch (args[1].ToLower())
		{
			//Triangle Formation
			case "triangle":
				player.ControlledBrain.Body.Formation = EPetFormationType.Triangle;
				break;
			//Line formation
			case "line":
				player.ControlledBrain.Body.Formation = EPetFormationType.Line;
				break;
			//Protect formation
			case "protect":
				player.ControlledBrain.Body.Formation = EPetFormationType.Protect;
				break;
			default:
				client.Out.SendMessage("Unrecognized argument: " + args[1], EChatType.CT_System, EChatLoc.CL_SystemWindow);
				break;
		}
	}
}