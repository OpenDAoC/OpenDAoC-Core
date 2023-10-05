namespace DOL.GS.Commands;

[Command(
  "&anonymous",
  new [] {"&anon"},
  ePrivLevel.Player,
  // Displays next to the command when '/cmd' is entered
  "Enables/disables anonymous mode, which hides you from player searches (e.g., '/who').",
  // Syntax: '/anonymous' or '/anon' - Enables/disables anonymous mode, which hides you from player searches (e.g., '/who').
  "PLCommands.Anonymous.Syntax.Anon")]
public class AnonymousCommand : ACommandHandler, ICommandHandler
{
	/// <summary>
	/// Change Player Anonymous Flag on Command
	/// </summary>
	/// <param name="client"></param>
	/// <param name="args"></param>
	public void OnCommand(GameClient client, string[] args)
	{
		if (client.Player == null)
			return;
		
		// If anonymous mode is disabled from the 'serverproperty' table
		if (client.Account.PrivLevel == 1 && ServerProperties.Properties.ANON_MODIFIER == -1)
		{
			// Message: Anonymous mode is currently disabled.
			ChatUtil.SendSystemMessage(client, "PLCommands.Anonymous.Err.Disabled", null);
			return;
		}

		// Sets the default value for anonymous mode on a character (off)
		client.Player.IsAnonymous = !client.Player.IsAnonymous;

		// Enable anonymous mode
		if (client.Player.IsAnonymous)
			// Message: You are now anonymous.
			ChatUtil.SendErrorMessage(client, "PLCommands.Anonymous.Msg.On", null);
		// Disable anonymous mode
		else
			// Message: You are no longer anonymous.
			ChatUtil.SendErrorMessage(client, "PLCommands.Anonymous.Msg.Off", null);
	}
}