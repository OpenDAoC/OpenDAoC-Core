using System;
using Core.GS.Enums;
using Core.GS.GameUtils;
using Core.GS.PacketHandler;

namespace Core.GS.Commands;

[Command(
	"&roll",
	EPrivLevel.Player,
	"simulates a dice roll.",
	"/roll [#] to throw with a specified number of dice")]
public class RollCommand : ACommandHandler, ICommandHandler
{
	// declaring some msg's
	private const int RESULT_RANGE = 512; // emote range
	private const int MAX_DICE = 100;
	private const int ONE_DIE_MAX_VALUE = 6; // :)
	private const string MESSAGE_HELP = "You must select the number of dice to roll!";
	private const string MESSAGE_RESULT_SELF = "You roll {0} dice and come up with: {1}"; // dice, thrown
	private const string MESSAGE_RESULT_OTHER = "{0} rolls {1} dice and comes up with: {2}"; // client.Player.Name, dice, thrown
	private readonly string MESSAGE_WRONG_NUMBER = "You must number of dice between 1 and " + MAX_DICE + "!";

	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "roll", 500))
		{
			DisplayMessage(client, "Slow down!");
			return;
		}

		// no args - display usage
		if (args.Length < 2)
		{
			SystemMessage(client, MESSAGE_HELP);
			return;
		}


		int dice; // number of dice to roll

		// trying to convert number
		try
		{
			dice = System.Convert.ToInt32(args[1]);
		}
		catch (OverflowException)
		{
			SystemMessage(client, MESSAGE_WRONG_NUMBER);
			return;
		}
		catch (Exception)
		{
			SystemMessage(client, MESSAGE_HELP);
			return;
		}

		if (dice < 1 || dice > MAX_DICE)
		{
			SystemMessage(client, MESSAGE_WRONG_NUMBER);
			return;
		}

		// throw result
		int thrown = Util.Random(dice, dice * ONE_DIE_MAX_VALUE);

		// building roll result msg
		string selfMessage = String.Format(MESSAGE_RESULT_SELF, dice, thrown);
		string otherMessage = String.Format(MESSAGE_RESULT_OTHER, client.Player.Name, dice, thrown);

		// sending msg to player
		EmoteMessage(client, selfMessage);

		// sending result & playername to all players in range
		foreach (GamePlayer player in client.Player.GetPlayersInRadius(RESULT_RANGE))
		{
			if (client.Player != player) // client gets unique message
				EmoteMessage(player, otherMessage); // sending msg to other players
		}
	}

	// these are to make code look better
	private void SystemMessage(GameClient client, string str)
	{
		client.Out.SendMessage(str, EChatType.CT_System, EChatLoc.CL_SystemWindow);
	}

	private void EmoteMessage(GamePlayer player, string str)
	{
		EmoteMessage(player.Client, str);
	}

	private void EmoteMessage(GameClient client, string str)
	{
		client.Out.SendMessage(str, EChatType.CT_Emote, EChatLoc.CL_SystemWindow);
	}
}