using System;
using DOL.GS.PacketHandler;

namespace DOL.GS.Commands;

[Command(
	"&random",
	EPrivLevel.Player,
	"prints out a random number between 1 and the number specified.",
	"/random [#] to get a random number between 1 and the number you specified.")]
public class RandomCommand : ACommandHandler, ICommandHandler
{
	// declaring some msg's
	private const int RESULT_RANGE = 512; // emote range
	private const string MESSAGE_HELP = "You must select a maximum number for your random selection!";
	private const string MESSAGE_RESULT_SELF = "You pick a random number between 1 and {0}: {1}"; // thrownMax, thrown
	private const string MESSAGE_RESULT_OTHER = "{0} picks a random number between 1 and {1}: {2}"; // client.Player.Name, thrownMax, thrown
	private const string MESSAGE_LOW_NUMBER = "You must select a maximum number greater than 1!";

	public void OnCommand(GameClient client, string[] args)
	{
		if (IsSpammingCommand(client.Player, "random", 500))
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

		int thrownMax;

		// trying to convert number
		try
		{
			thrownMax = System.Convert.ToInt32(args[1]);
		}
		catch (OverflowException)
		{
			thrownMax = int.MaxValue - 1; // max+1 is used in GameObject.Random(int,int)
		}
		catch (Exception)
		{
			SystemMessage(client, MESSAGE_HELP);
			return;
		}

		if (thrownMax < 2)
		{
			SystemMessage(client, MESSAGE_LOW_NUMBER);
			return;
		}

		// throw result
		int thrown = Util.Random(1, thrownMax);
		
		BattleGroupUtil mybattlegroup = client.Player.TempProperties.GetProperty<BattleGroupUtil>(BattleGroupUtil.BATTLEGROUP_PROPERTY, null);
		if (mybattlegroup != null && mybattlegroup.IsRecordingRolls() && thrownMax <= mybattlegroup.GetRecordingThreshold())
		{
			mybattlegroup.AddRoll(client.Player, thrown);
		}

		// building result messages
		string selfMessage = String.Format(MESSAGE_RESULT_SELF, thrownMax, thrown);
		string otherMessage = String.Format(MESSAGE_RESULT_OTHER, client.Player.Name, thrownMax, thrown);

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
		client.Out.SendMessage(str, eChatType.CT_System, eChatLoc.CL_SystemWindow);
	}

	private void EmoteMessage(GamePlayer player, string str)
	{
		EmoteMessage(player.Client, str);
	}

	private void EmoteMessage(GameClient client, string str)
	{
		client.Out.SendMessage(str, eChatType.CT_Emote, eChatLoc.CL_SystemWindow);
	}
}