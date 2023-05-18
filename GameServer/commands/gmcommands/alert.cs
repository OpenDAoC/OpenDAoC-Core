/*
 * DAWN OF LIGHT - The first free open source DAoC server emulator
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 *
 */

namespace DOL.GS.Commands
{
	[CmdAttribute(
		// Enter '/alert' to list all commands of this type
		"&alert",
		// Message: '/alert' - Controls whether sound alerts are triggered when receiving Player messages and appeals.
		"GMCommands.Alert.CmdList.Description",
		// Message: <----- '/{0}' Command {1}----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.GM,
		// Message: Controls whether sound alerts are triggered when receiving Player messages and appeals.
		"GMCommands.Alert.Description",
		// Syntax: /alert all < on | off >
		"GMCommands.Alert.Syntax.All",
		// Message: Activates/Deactivates sound alerts for all alert types.
		"GMCommands.Alert.Usage.All",
		// Syntax: /alert send < on | off >
		"GMCommands.Alert.Syntax.Send",
		// Message: Activates/Deactivates a sound alert each time a '/send' message is received from a Player.
		"GMCommands.Alert.Usage.Send",
		// Syntax: /alert appeal < on | off >
		"GMCommands.Alert.Syntax.Appeal",
		// Message: Activates/Deactivates a sound alert each time an '/appeal' is submitted or pending assistance.
		"GMCommands.Alert.Usage.Appeal"
	)]
	public class AlertCommandHandler : AbstractCommandHandler, ICommandHandler
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
						client.Player.TempProperties.setProperty("AppealAlert", false);
						client.Player.TempProperties.setProperty("SendAlert", false);
						
						// Message: You will now receive sound alerts.
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.AllOn", null);
					}
					if (args[2] == "off")
					{
						client.Player.TempProperties.setProperty("AppealAlert", true);
						client.Player.TempProperties.setProperty("SendAlert", true);
						
						// Message: You will no longer receive sound alerts.
						ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.AllOff", null);
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
							client.Player.TempProperties.setProperty("AppealAlert", false);
							
							// Message: You will now receive sound alerts when an appeal is filed or awaiting assistance.
							ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.AppealOn", null);
						}
						if (args[2] == "off")
						{
							client.Player.TempProperties.setProperty("AppealAlert", true);
							
							// Message: You will no longer receive sound alerts regarding appeals.
							ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.AppealOff", null);
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
							client.Player.TempProperties.setProperty("SendAlert", false);
							
							// Message: You will now receive sound alerts when a player sends you a message.
							ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.SendOn", null);
						}
						if (args[2] == "off")
						{
							client.Player.TempProperties.setProperty("SendAlert", true);
							
							// Message: You will no longer receive sound alerts for player messages.
							ChatUtil.SendTypeMessage((int)eMsg.Success, client, "GMCommands.Alert.Msg.SendOff", null);
						}
						return;
					}
				#endregion Send
				
			}
		}
	}
}