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
using System.Reflection;
using log4net;

namespace DOL.GS.Commands
{
	/// <summary>
	/// Providing some basic command handler functionality
	/// </summary>
	public abstract class AbstractCommandHandler
	{
		private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		
		/// <summary>
		/// Is this player spamming this command
		/// </summary>
		/// <param name="player">The player performing the command</param>
		/// <param name="commandName">The command being performed</param>
		/// <returns></returns>
		public bool IsSpammingCommand(GamePlayer player, string commandName)
		{
			return IsSpammingCommand(player, commandName, ServerProperties.Properties.COMMAND_SPAM_DELAY);
		}

		/// <summary>
		/// Is this player spamming this command
		/// </summary>
		/// <param name="player">The player performing the command</param>
		/// <param name="commandName">The command being performed</param>
		/// <param name="delay">How long is the spam delay in milliseconds</param>
		/// <returns>true if less than spam protection interval</returns>
		public bool IsSpammingCommand(GamePlayer player, string commandName, int delay)
		{
			string spamKey = commandName + "NOSPAM";
			long tick = player.TempProperties.getProperty<long>(spamKey, 0);

			if (tick > 0 && player.CurrentRegion.Time - tick <= 0)
			{
				player.TempProperties.removeProperty(spamKey);
			}

			long changeTime = player.CurrentRegion.Time - tick;

			if (tick > 0 && (player.CurrentRegion.Time - tick) < delay)
			{
				return true;
			}

			player.TempProperties.setProperty(spamKey, player.CurrentRegion.Time);
			return false;
		}

		public virtual void DisplayMessage(GamePlayer player, string message)
		{
			DisplayMessage(player.Client, message, new object[] {});
		}

		public virtual void DisplayMessage(GameClient client, string message)
		{
			DisplayMessage(client, message, new object[] {});
		}

		public virtual void DisplayMessage(GameClient client, string message, params object[] objs)
		{
			if (client == null || !client.IsPlaying)
				return;

			ChatUtil.SendTypeMessage(eMsg.System, client, string.Format(message, objs));
			return;
		}

		/// <summary>
		/// Displays all attribute messages for a command
		/// </summary>
		/// <param name="client">The recipient of the messages (e.g., client)</param>
		/// <returns>Messages about a command's syntax and usage</returns>
		/// <example>DisplaySyntax(client);</example>
		public virtual void DisplaySyntax(GameClient client)
		{
			if (client == null || !client.IsPlaying)
				return;

			var attrib = (CmdAttribute[]) GetType().GetCustomAttributes(typeof (CmdAttribute), false);
			
			if (attrib.Length == 0)
				return;

			var plvlReq = "(plvl " + (int)attrib[0].Level + ") ";
			var command = attrib[0].Cmd.Remove(0, 1);

			// Only include this on GM/Admin commands until Player commands are "ready"
			if (client.Account.PrivLevel > 1)
			{
				// Include header/divider at head of return upon typing the main command identifier or alias (e.g., '/command')
				if (attrib[0].Header != "")
					// Message: 
					ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, attrib[0].Header, command, plvlReq);
				else
					// Message: <----- '/{0}' Command ----->
					ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AllCommands.Header.Basic.Commands", command);
			}
			
			// Include main command type description below the header
			// Example: Creates new, manages existing, and controls character assignment for OpenDAoC accounts.
			ChatUtil.SendTypeMessage(eMsg.CmdDesc, client, attrib[0].Description, null);
			// Line break
			ChatUtil.SendTypeMessage(eMsg.Command, client, " ");
			// Message: Use the following syntax for this command:
			ChatUtil.SendTypeMessage(eMsg.Command, client, "AllCommands.Text.General.UseSyntax", null);

			// Run for each value found under params until the whole list of subcommands is displayed
			foreach (var sentence in attrib[0].Usage)
			{
				// To contrast the appearance of command syntax against their descriptions, include ".Syntax." in the translation ID (e.g., "AdminCommands.Account.Syntax.AccountName")
				if (sentence.Contains(".Syntax."))
				{
					// Example: /account changepassword <accountName> <newPassword>
					ChatUtil.SendTypeMessage(eMsg.CmdSyntax, client, sentence, null);
				}
				// All other values display as command descriptions (i.e., CT_System)
				else
				{
					// Example: Changes the password associated with an existing account. If a player requests a password reset, verify ownership of the account.
					ChatUtil.SendTypeMessage(eMsg.CmdUsage, client, sentence, null);
				}
			}
		}
		
		/// <summary>
		/// Displays messages that describe the syntax and behaviors of a command or subcommand
		/// </summary>
		/// <param name="client">The recipient of the messages (e.g., client)</param>
		/// <param name="command">The full base command (e.g., "plvl")</param>
		/// <param name="subcommand">The full subcommand (e.g., "remove")--leave blank ("") for base command</param>
		/// <param name="plvl">The privilege level required to use the command (e.g., 3)</param>
		/// <param name="database">Specifies whether this command may be accomplished using the Atlas Admin tool (e.g., true)</param>
		/// <param name="syntaxID">The translation ID associated with the subcommand's syntax (e.g., "AdminCommands.Account.Syntax.Comm")</param>
		/// <param name="usageID">The translation ID associated with the subcommand's description (e.g., "AdminCommands.Account.Usage.Comm")</param>
		/// <returns>Messages about a command's syntax and usage</returns>
		/// <example>DisplayHeadSyntax(client, "plvl", "remove", 3, false, "AdminCommands.Account.Syntax.Comm", "AdminCommands.Account.Usage.Comm");</example>
		public void DisplayHeadSyntax(GameClient client, string command, string subcommand, string subcommand2, int plvl, bool database, string syntaxID, string usageID)
		{
			if (client == null || !client.IsPlaying)
				return;
			
			var plvlReq = "(plvl " + plvl + ") "; // Attach plvl to header
			var subVar = " " + subcommand; // Displays for {1} on subcommand message
			var subVar2 = " " + subcommand2; // Displays for {1} on subcommand message
			
			// Don't show plvl requirements to Players (plvl shouldn't exist to them)
			if (plvl == 1 && client.Account.PrivLevel == 1)
				plvlReq = "";

			if (subcommand != "")
			{
				if (subcommand2 != "")
					// Message: <----- '/{0}{1}{2}' Subcommand {3}----->
					ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AllCommands.Header.General.2Subcommand", command, subVar, subVar2, plvlReq);
				else
					// Message: <----- '/{0}{1}' Subcommand {2}----->
					ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AllCommands.Header.General.Subcommand", command, subVar, plvlReq);
			}
			
			if (subcommand == "")
				// Message: <----- '/{0}' Command {2}----->
				ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, "AllCommands.Header.General.Commands", command, plvlReq);
			
			if (database == false || client.Account.PrivLevel == 1)
				// Message: Use the following syntax for this command:
				ChatUtil.SendTypeMessage(eMsg.Command, client, "AllCommands.Text.General.UseSyntax", null);
				
			if (database && client.Account.PrivLevel > 1)
				// Message: It is recommended that you perform actions associated with this command with the Atlas Admin (https://admin.atlasfreeshard.com). Otherwise, use the following syntax:
				ChatUtil.SendTypeMessage(eMsg.Command, client, "AllCommands.Text.General.SyntaxDB", null);
			
			// Example: /account command
			ChatUtil.SendTypeMessage(eMsg.CmdSyntax, client, syntaxID, null);
			// Example: Provides additional information regarding the '/account' command type.
			ChatUtil.SendTypeMessage(eMsg.CmdUsage, client, usageID, null);
		}
		
		/// <summary>
		/// Displays messages that describe the syntax and behaviors of a subcommand, without the header strings
		/// </summary>
		/// <param name="client">The recipient of the messages (e.g., client)</param>
		/// <param name="syntaxID">The translation ID associated with the subcommand's syntax (e.g., "AdminCommands.Account.Syntax.Comm")</param>
		/// <param name="usageID">The translation ID associated with the subcommand's description (e.g., "AdminCommands.Account.Usage.Comm")</param>
		/// <returns>Messages about a subcommand's syntax</returns>
		/// <example>DisplaySubSyntax(client, "AdminCommands.Account.Syntax.Comm", "AdminCommands.Account.Usage.Comm");</example>
		public void DisplaySubSyntax(GameClient client, string syntaxID, string usageID)
		{
			if (client == null || !client.IsPlaying)
				return;
			
			// Example: /account command
			ChatUtil.SendTypeMessage(eMsg.CmdSyntax, client, syntaxID, null);
			// Example: Provides additional information regarding the '/account' command type.
			ChatUtil.SendTypeMessage(eMsg.CmdUsage, client, usageID, null);
		}
		
		public void DisplayHeaderSyntax(GameClient client, string headerID)
		{
			if (client == null || !client.IsPlaying)
				return;
			
			// Example: /account command
			ChatUtil.SendTypeMessage(eMsg.CmdHeader, client, headerID, null);
		}
		
		public void DisplaySyntaxSyntax(GameClient client, string syntaxID)
		{
			if (client == null || !client.IsPlaying)
				return;
			
			// Example: /account command
			ChatUtil.SendTypeMessage(eMsg.CmdSyntax, client, syntaxID, null);
		}
		
		public void DisplayUsageSyntax(GameClient client, string usageID)
		{
			if (client == null || !client.IsPlaying)
				return;
			
			// Example: Provides additional information regarding the '/account' command type. 
			ChatUtil.SendTypeMessage(eMsg.CmdUsage, client, usageID, null);
		}

		public virtual void DisplaySyntax(GameClient client, string subcommand)
		{
			if (client == null || !client.IsPlaying)
				return;

			var attrib = (CmdAttribute[]) GetType().GetCustomAttributes(typeof (CmdAttribute), false);

			if (attrib.Length == 0)
				return;

			foreach (string sentence in attrib[0].Usage)
			{
				string[] words = sentence.Split(new[] {' '}, 3);

				if (words.Length >= 2 && words[1].Equals(subcommand))
				{
					ChatUtil.SendSystemMessage(client, sentence, null);
				}
			}

			return;
		}

		public virtual void DisplaySyntax(GameClient client, string subcommand1, string subcommand2)
		{
			if (client == null || !client.IsPlaying)
				return;

			var attrib = (CmdAttribute[]) GetType().GetCustomAttributes(typeof (CmdAttribute), false);

			if (attrib.Length == 0)
				return;

			foreach (string sentence in attrib[0].Usage)
			{
				string[] words = sentence.Split(new[] {' '}, 4);

				if (words.Length >= 3 && words[1].Equals(subcommand1) && words[2].Equals(subcommand2))
				{
					ChatUtil.SendSystemMessage(client, sentence, null);
				}
			}

			return;
		}
	}
}