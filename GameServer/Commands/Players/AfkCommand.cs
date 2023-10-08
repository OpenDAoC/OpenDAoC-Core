namespace DOL.GS.Commands;

[Command(
	"&afk",
	EPrivLevel.Player,
	// Displays next to the command when '/cmd' is entered
	"Enables/disables a flag that indicates you are \"away from keyboard,\" and allows you to attach a message that will auto-send to any player that uses '/send' to you (e.g., '/afk Bio break').",
	// Syntax: '/afk' - Enables/disables a flag that indicates you are "away from keyboard," and allows you to attach a message that will auto-send to any player that uses '/send' to you (e.g., '/afk Bio break').
	"PLCommands.AFK.Syntax.AFK",
	// Syntax: '/afk <message>' - Sets yourself as "away from keyboard," and attaches a message that will auto-send to any player that uses '/send' to you.
	"PLCommands.AFK.Syntax.MessageAFK")]
public class AfkCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player.TempProperties.GetProperty<string>(GamePlayer.AFK_MESSAGE) != null && args.Length == 1)
		{
			client.Player.TempProperties.RemoveProperty(GamePlayer.AFK_MESSAGE);
			// Message: Your AFK flag is now off.
			ChatUtil.SendErrorMessage(client, "PLCommands.AFK.Msg.Off", null);
		}
		else
		{
			if (args.Length > 1)
			{
				string message = string.Join(" ", args, 1, args.Length - 1);
				client.Player.TempProperties.SetProperty(GamePlayer.AFK_MESSAGE, message);
			}
			else
			{
				client.Player.TempProperties.SetProperty(GamePlayer.AFK_MESSAGE, "");
			}
			
			// Message: Your AFK flag is now on.
			ChatUtil.SendErrorMessage(client, "PLCommands.AFK.Msg.On", null);
		}
	}
}