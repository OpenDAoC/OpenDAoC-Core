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

using System.Collections.Generic;
using DOL.GS.PacketHandler;
using DOL.Language;

namespace DOL.GS.Commands
{
	// See the comments above 'using' about SendMessage translation IDs
	[CmdAttribute(
		// Enter '/plvl' to list all associated subcommands
		"&plvl",
		// Message: '/plvl' - Alters an account's privilege level (plvl) and grants/revokes access to command types depending on the user's plvl.
		"AdminCommands.Plvl.CmdList.Description",
		// Message: <----- '/plvl' Commands (plvl 3) ----->
		"AllCommands.Header.General.Commands",
		// Required minimum privilege level to use the command
		ePrivLevel.Admin,
		// Message: Alters an account's privilege level (plvl) and grants/revokes access to command types depending on the user's plvl. With these commands, accounts may be granted Admin, GM, and Player command access from in-game. These commands are intended for testing purposes and should not be used on non-staff accounts.
		"AdminCommands.Plvl.Description",
		// Syntax: /plvl command
		"AdminCommands.Plvl.Syntax.Comm",
		// Message: Provides additional information regarding the '/plvl' command type.
		"AdminCommands.Plvl.Usage.Comm",
		// Syntax: /plvl <newPlvl> <playerName>
		"AdminCommands.Plvl.Syntax.Plvl",
		// Message: Sets the privilege level for a targeted player's account. The player will then have access to all commands associated with that privilege level. Use '/plvl single' or '/plvl singleaccount' to grant access to specific commands as a Player.
		"AdminCommands.Plvl.Usage.Plvl",
		// Syntax: /plvl remove <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.Remove",
		// Message: Removes a specific permission previously granted to a player using the '/plvl single' command.
		"AdminCommands.Plvl.Usage.Remove",
		// Syntax: /plvl removeaccount <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.AcctRemove",
		// Message: Removes a specific permission previously granted to a player's account using the '/plvl singleaccount' command.
		"AdminCommands.Plvl.Usage.AcctRemove",
		// Syntax: /plvl single <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.Single",
		// Message: Grants a character the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
		"AdminCommands.Plvl.Usage.Single",
		// Syntax: /plvl singleaccount <commandType> <playerName>
		"AdminCommands.Plvl.Syntax.AcctSingle",
		// Message: Grants all characters on a player's account the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
		"AdminCommands.Plvl.Usage.AcctSingle"
	)]
	
	public class PlvlCommand : AbstractCommandHandler, ICommandHandler
	{
		public void OnCommand(GameClient client, string[] args)
		{
			if (args.Length < 2)
			{
				// Lists '/plvl' commands' syntax (see section above)
				DisplaySyntax(client);
				return;
			}

			GamePlayer target = client.Player;

			switch (args[1])
			{
				#region Single
				// Grants a single command type to the specified player that may be used no matter their privilege level
				// Syntax: /plvl single <commandType> <playerName>
				// See the comments above 'using' about SendMessage translation IDs
				case "single":
				{
					switch (args.Length)
					{
						// Display syntax for '/plvl single'
						case 2:
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: Use the following syntax for this command:
							// Syntax: /plvl single <commandType> <playerName>
							// Message: Grants a character the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
							DisplayHeadSyntax(client, "plvl", "single", "", 3, false, "AdminCommands.Plvl.Syntax.Single", "AdminCommands.Plvl.Usage.Single");
							return;
						}
						// Player is not specified
						case 3:
						{
							// Message: [ERROR] You must specify a player name for this command.
							ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.SpecifyName", null);
							return;
						}
						// Player name specified
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							if (targetClient == null)
							{
								// Message: [ERROR] No character is online with the name '{0}'.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AllCommands.Command.Err.NoOnlineChar", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// Adds the command type to the 'singlepermission' table
							SinglePermission.setPermission(targetClient.Player, args[2]);
							// Message: [SUCCESS] You have granted {0} access to the '/{1}' command!
							ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.Plvl.Msg.AddSinglePerm", target.Name, args[2]);

							if (target != client.Player)
							{
								// Message: {0} has given you access to the '/{1}' command! Type '/{1}' to view the command list.
								ChatUtil.SendTypeMessage(eMsg.Staff, target.Client, "AdminCommands.Plvl.Msg.GaveSinglePerm", client.Player.Name, args[2]);
							}
							return;
						}
					}
					return;
				}
				#endregion Single
				#region Single Account
				// Grants a command type to all existing characters on the specified player's account
				// Syntax: /plvl singleaccount <commandType> <playerName>
				// See the comments above 'using' about SendMessage translation IDs
				case "singleaccount":
				{
					switch (args.Length)
					{
						// If only '/plvl singleaccount' is entered
						case 2:
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: Use the following syntax for this command:
							// Syntax: /plvl singleaccount <commandType> <playerName>
							// Message: Grants all characters on a player's account the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
							DisplayHeadSyntax(client, "plvl", "singleaccount", "", 3, false, "AdminCommands.Plvl.Syntax.AcctSingle", "AdminCommands.Plvl.Usage.AcctSingle");
							return;
						}
						// If only '/plvl singleaccount <commandName>' is entered
						case 3:
						{
							// Message: [ERROR] You must specify a player name for this command.
							ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.SpecifyName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							if (targetClient == null)
							{
								// Message: [ERROR] No character is online with the name '{0}'.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.NoOnlineChar", null);
								return;
							}

							target = targetClient.Player;

							SinglePermission.setPermissionAccount(target, args[2]);
							// Message: [SUCCESS] You have granted {0}'s account access to the '/{1}' command!
							ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.Plvl.Msg.AddAcct", target.Name, args[2]);

							// Sends message to target
							if (target != client.Player)
							{
								// Message: {0} has given your account access to the '/{1}' command! Type '/{1}' to view the command list.
								ChatUtil.SendTypeMessage(eMsg.Staff, target.Client, "AdminCommands.Plvl.Msg.GaveAcct", client.Player.Name, args[2]);
							}
							return;
						}
					}
					return;
				}
				#endregion Single Account
				#region Remove
				// Revokes a command type from the specified character
				// Syntax: /plvl remove <commandType> <playerName>
				// See the comments above 'using' about SendMessage translation IDs
				case "remove":
				{
					switch (args.Length)
					{
						// If only '/plvl remove' is entered
						case 2:
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: Use the following syntax for this command:
							// Syntax: /plvl remove <commandType> <playerName>
							// Message: Removes a specific command type previously granted to a player using '/plvl single'.
							DisplayHeadSyntax(client, "plvl", "remove", "", 3, false, "AdminCommands.Plvl.Syntax.Remove", "AdminCommands.Plvl.Usage.Remove");
							return;
						}
						// If only '/plvl remove <commandType>' is entered
						case 3:
						{
							// Message: [ERROR] You must specify a player name for this command.
							ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.SpecifyName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							// If the account doesn't exist
							if (targetClient == null)
							{
								// Message: [ERROR] No character is online with the name '{0}'.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.NoOnlineChar", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// If player has permission, remove it
							if (SinglePermission.HasPermission(target, args[2]))
							{
								// Removes the command type to the 'singlepermission' table
								SinglePermission.removePermission(target, args[2]);
							}
							// If player doesn't have permission
							else
							{
								// Message: [ERROR] No permission has been granted to {0} for the '/{1}' command.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Plvl.Err.NoPermFound",
									target.Name, args[2]);
								return;
							}

							// Message: [SUCCESS] You have revoked {0}'s access to the '/{1}' command!
							ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.Plvl.Msg.RevokeSinglePerm", target.Name, args[2]);

							// If the target isn't player executing command
							if (target != client.Player)
							{
								// Message: {0} has removed your access to the '/{1}' command! You may no longer use this command type while assigned the Player privilege level.
								ChatUtil.SendTypeMessage(eMsg.Staff, target, "AdminCommands.Plvl.Msg.DelSinglePerm", client.Player.Name, args[2]);
								return;
							}
						}
						return;
					}
					break;
				}
				#endregion Remove
				#region Remove Account
				// Revokes a command type from the specified character's account
				// Syntax: /plvl removeaccount <commandType> <playerName>
				// See the comments above 'using' about SendMessage translation IDs
                case "removeaccount":
                {
	                switch (args.Length)
					{
						// If '/plvl removeaccount' is entered
						case 2:
						{
							// Message: <----- '/{0}{1}' Subcommand {2}----->
							// Message: Use the following syntax for this command:
							// Syntax: /plvl removeaccount <commandType> <playerName>
							// Message: Removes a specific permission previously granted to a player's account using '/plvl singleaccount'.
							DisplayHeadSyntax(client, "plvl", "removeaccount", "", 3, false, "AdminCommands.Plvl.Syntax.AcctRemove", "AdminCommands.Plvl.Usage.AcctRemove");
							return;
						}
						// If '/plvl removeaccount <commandType>' is entered
						case 3:
						{
							// Message: [ERROR] You must specify a player name for this command.
							ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.SpecifyName", null);
							return;
						}
						// If full command is entered
						case >= 4:
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[3], true, true);

							// If the account doesn't exist
							if (targetClient == null)
							{
								// Message: [ERROR] No character is online with the name '{0}'.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.NoOnlineChar", args[3]);
								return;
							}

							// Target is the player logged in
							target = targetClient.Player;

							// If player's account has the permission, remove it from the 'singlepermission' table
							if (SinglePermission.HasPermission(target, args[2]))
							{
								SinglePermission.removePermissionAccount(target, args[2]);
							}
							// If player's account doesn't have permission
							else
							{
								// Message: [ERROR] No permission was found for {0}'s account and the '/{1}" command!
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Plvl.Err.NoAcctPermFound", target.Name, args[2]);
								return;
							}

							// Message: [SUCCESS] You have revoked {0}'s account access to the '/{1}' command!
							ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.Plvl.Msg.RevokeAcctPerm", target.Name, args[2]);

							// If the target isn't player executing command
							if (target != client.Player)
							{
								// Message: {0} has removed your account's access to the '/{1}' command! Your characters may no longer use this command type while assigned the Player privilege level.
								ChatUtil.SendTypeMessage(eMsg.Staff, target.Client, "AdminCommands.Plvl.Msg.DelAcctPerm", client.Player.Name, args[2]);
								return;
							}
						}
						return;
					}
	                break;
                }
				#endregion Remove Account
				#region Plvl
				// Sets the privilege level for a player's account
				// Syntax: /plvl <newPlvl> <playerName>
				// See the comments above 'using' about SendMessage translation IDs
				default:
					{
						uint plvl = 1;

						// If an unsupported value is entered in place of '<newPlvl>'
						if (!uint.TryParse(args[1], out plvl))
						{
							// Message: <----- '/{0}' Command {1}----->
							// Message: Use the following syntax for this command:
							// Syntax: /plvl <newPlvl> <playerName>
							// Message: Sets the privilege level for a targeted player's account. They will then have access to all commands associated with that role. Use '/plvl single' or '/plvl singleaccount' to retain access to specific command types as a Player.
							DisplayHeadSyntax(client, "plvl", "", "", 3, true, "AdminCommands.Plvl.Syntax.Plvl", "AdminCommands.Plvl.Usage.Plvl");
							return;
						}
						// If a player name is specified
						if (args.Length > 2)
						{
							GameClient targetClient = WorldMgr.GetClientByPlayerName(args[2], true, true);

							if (targetClient == null) 
							{
								// Message: [ERROR] No character is online with the name '{0}'.
								ChatUtil.SendTypeMessage(eMsg.Error, client, "AdminCommands.Command.Err.NoOnlineChar", args[2]);
								return;
							}
							
							target = targetClient.Player;
						}
						// If plvl specified is Player or GM and no target is specified
						if (args[1] == "1" || args[1] == "2" && client.Player == target && target == null)
						{
							// If player's account doesn't have 'plvl' permission
							if (SinglePermission.HasPermission(client.Player, "plvl") == false)
							{
								// Message: You do not have the '/plvl' permission assigned! You will be unable to access the '/plvl' command again as a GM or Player.
								ChatUtil.SendTypeMessage(eMsg.DialogWarn, client, "AdminCommands.Plvl.Err.NoPlvlPerm", null);
								return;
							}
						}
						
						if (target != null)
						{
							target.Client.Account.PrivLevel = plvl;

							// Sets the privilege level to the character's account, saves to DB, and refreshes world for character to reflect the change
							GameServer.Database.SaveObject(target.Client.Account);
							client.Player.RefreshWorld();
							// Refresh equipment for player so they don't appear naked after changing plvl
							client.Player.UpdateEquipmentAppearance();

							// Message: [SUCCESS] You have changed {0}'s account privilege level to {1}!
							ChatUtil.SendTypeMessage(eMsg.Success, client, "AdminCommands.Plvl.Msg.PlvlSet", target.Name, plvl.ToString());

							if (target != client.Player)
								// Message: {0} has changed your account's privilege level to {1}!
								ChatUtil.SendTypeMessage(eMsg.Staff, target.Client, "AdminCommands.Plvl.Msg.YourPlvlSet", client.Player.Name, plvl.ToString());
						}
						break;
					}
				#endregion Plvl
				#region Command
				// Provides additional information regarding the '/plvl' command type
				// Syntax: /plvl command
				// See the comments above 'using' about SendMessage translation IDs
				case "command":
					{
						ChatUtil.SendWindowMessage(eWindow.Text, client, "Using the '/plvl' Command",
							" ", 
							// Message: ----- Privilege Levels -----
							"Dialog.Header.Content.PrivLevels", 
							" ", 
							" ", 
							// Message: The '/plvl' command type allows you to control an account's privilege level and the command types its characters may access when they are a Player. The values used for each plvl are:
							"AdminCommands.Plvl.Comm.Intro", 
							" ", 
							// Message: 1 = Player
							"AdminCommands.Plvl.Comm.1", 
							// Message: You can be attacked by mobs or other players and take falling damage. Players only have access to basic slash commands (such as '/bg', '/gc', '/cg', etc.).
							"AdminCommands.Plvl.Comm.Usage1", 
							" ", 
							// Message: 2 = Gamemaster (GM)
							"AdminCommands.Plvl.Comm.2", 
							// Message: You cannot be attacked by mobs or other players or take falling damage. GMs have access to MOST special slash commands (except those requiring ePrivLevel.Admin--such as '/plvl', '/account', '/shutdown', etc.).
							"AdminCommands.Plvl.Comm.Usage2",
							" ", 
							// Message: 3 = Admin
							"AdminCommands.Plvl.Comm.3", 
							// Message: You cannot be attacked by mobs or other players or take falling damage. Admins have access to ALL special slash commands (including GM).
							"AdminCommands.Plvl.Comm.Usage3", 
							" ", 
							// Message: These integers are used when changing your priv level ('/plvl <newPlvl> <playerName>') to test functionality or combat in-game.
							"AdminCommands.Plvl.Comm.UseSingleAcct", 
							" ",
							" ",
							// Message: ----- NOTE -----
							"AllCommands.Header.Note.Divider",
							" ",
							// Message: To retain access to specific command types as a Player, enter '/plvl single' or '/plvl singleaccount' for details.
							"AdminCommands.Plvl.Note.PlvlSingle", 
							" ",
							" ",
							// Message: ----- '/plvl' Commands (plvl 3) -----
							"AdminCommands.Header.Syntax.Plvl", 
							" ", 
							// Message: Alters an account's privilege level (plvl) and grants/revokes access to command types depending on the user's plvl. With these commands, accounts may be granted Admin, GM, and Player command access from in-game. These commands are intended for testing purposes and should not be used on non-staff accounts.
							"AdminCommands.Plvl.Description",
							" ",
							// Message: /plvl command
							"AdminCommands.Plvl.Syntax.Comm",
							// Message: Provides additional information regarding the '/plvl' command type.
							"AdminCommands.Plvl.Usage.Comm",
							" ",
							// Message: /plvl <newPlvl> <playerName>
							"AdminCommands.Plvl.Syntax.Plvl",
							// Message: Sets the privilege level for a targeted player's account. They will then have access to all commands associated with that role. Use '/plvl single' or '/plvl singleaccount' to retain access to specific command types as a Player.
							"AdminCommands.Plvl.Usage.Plvl",
							" ",
							// Message: /plvl remove <commandType> <playerName>
							"AdminCommands.Plvl.Syntax.Remove",
							// Message: Removes a specific command type previously granted to a player using '/plvl single'.
							"AdminCommands.Plvl.Usage.Remove",
							" ",
							// Message: /plvl removeaccount <commandType> <playerName>
							"AdminCommands.Plvl.Syntax.AcctRemove",
							// Message: Removes a specific command type previously granted to a player's account using '/plvl singleaccount'.
							"AdminCommands.Plvl.Usage.AcctRemove",
							" ",
							// Message: /plvl single <commandType> <playerName>
							"AdminCommands.Plvl.Syntax.Single",
							// Message: Grants a character the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
							"AdminCommands.Plvl.Usage.Single",
							" ",
							// Message: /plvl singleaccount <commandType> <playerName>
							"AdminCommands.Plvl.Syntax.AcctSingle",
							// Message: Grants all characters on a player's account the ability to perform a specific command type regardless of their current privilege level. For '<commandType>', enter only the command identifier (e.g., 'player' for '/player' commands, 'plvl' for '/plvl' commands, etc.).
							"AdminCommands.Plvl.Usage.AcctSingle",
							" ");
						return;
					}
				#endregion Command
				
			}
		}
	}
}