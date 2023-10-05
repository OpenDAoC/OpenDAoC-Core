namespace DOL.GS.Commands;

[Command(
	// Enter '/alert' to list all commands of this type
	"&alert",
	// Message: <----- '/alert' Commands (plvl 2) ----->
	"GMCommands.Header.Syntax.Alert",
	ePrivLevel.GM,
	// Message: "Controls whether sound alerts are triggered when receiving Player messages and appeals."
	"GMCommands.Alert.Description",
	// Syntax: /alert all < on | off >
	"GMCommands.Alert.Syntax.All",
	// Message: "Activates/Deactivates sound alerts for all alert types."
	"GMCommands.Alert.Usage.All",
	// Syntax: /alert send < on | off >
	"GMCommands.Alert.Syntax.Send",
	// Message: "Activates/Deactivates a sound alert each time a '/send' message is received from a Player."
	"GMCommands.Alert.Usage.Send",
	// Syntax: /alert appeal < on | off >
	"GMCommands.Alert.Syntax.Appeal",
	// Message: "Activates/Deactivates a sound alert each time an '/appeal' is submitted or pending assistance."
	"GMCommands.Alert.Usage.Appeal"
	)]
public class AlertCommand : ACommandHandler, ICommandHandler
{
	public void OnCommand(GameClient client, string[] args)
	{
		if (args.Length == 1)
		{
			// Lists all '/alert' subcommand syntax (see '&alert' above)
			DisplaySyntax(client);
			return;
		}

		switch (args[1].ToLower())
		{
			#region All
			// Triggers an audible alert for all existing alert types
			// Syntax: /alert all < on | off >
			// Args:   /alert args[1] args[2]
			// See the comments above 'using' about SendMessage translation IDs
			case "all":
			{
				if (args[2] == "on")
				{
					client.Player.TempProperties.SetProperty("AppealAlert", false);
					client.Player.TempProperties.SetProperty("SendAlert", false);
					// Message: "You will now receive sound alerts."
					ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.AllOn", null);
				}
				if (args[2] == "off")
				{
					client.Player.TempProperties.SetProperty("AppealAlert", true);
					client.Player.TempProperties.SetProperty("SendAlert", true);
					// Message: "You will no longer receive sound alerts."
					ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.AllOff", null);
				}
				return;
			}
			#endregion All
			
			#region Appeal
			// Triggers an audible alert when appeal is submitted or awaiting staff assistance
			// Syntax: /alert appeal < on | off >
			// Args:   /alert args[1] args[2]
			// See the comments above 'using' about SendMessage translation IDs
			case "appeal":
				{
					if (args[2] == "on")
					{
						client.Player.TempProperties.SetProperty("AppealAlert", false);
						// Message: "You will now receive sound alerts when an appeal is filed or awaiting assistance."
						ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.AppealOn", null);
					}
					if (args[2] == "off")
					{
						client.Player.TempProperties.SetProperty("AppealAlert", true);
						// Message: "You will no longer receive sound alerts regarding appeals."
						ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.AppealOff", null);
					}
					return;
				}
			#endregion Appeal
			
			#region Send
			// Triggers an audible alert when a Player sends a message to you
			// Syntax: /alert appeal < on | off >
			// Args:   /alert args[1] args[2]
			// See the comments above 'using' about SendMessage translation IDs
			case "send":
				{
					if (args[2] == "on")
					{
						client.Player.TempProperties.SetProperty("SendAlert", false);
						// Message: "You will now receive sound alerts when a player sends you a message."
						ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.SendOn", null);
					}
					if (args[2] == "off")
					{
						client.Player.TempProperties.SetProperty("SendAlert", true);
						// Message: "You will no longer receive sound alerts for player messages."
						ChatUtil.SendSystemMessage(client, "GMCommands.Alert.Msg.SendOff", null);
					}
					return;
				}
			#endregion Send
			
		}
	}
}